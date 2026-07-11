using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// Drives the project detail window. Holds the project graph as its source of truth,
/// rebuilding the phase sections / Kanban columns after every change and keeping the
/// Waterfall locks and progress bars in sync.
/// </summary>
public partial class ProjectDetailViewModel : ObservableObject
{
    private readonly IProjectService _projects;
    private readonly ITaskService _tasks;
    private readonly ISubtaskEditor _editor;
    private readonly IProjectExporter _exporter;
    private readonly bool _developerFeatures;

    [ObservableProperty]
    private int _overallPercent;

    [ObservableProperty]
    private double _overallFraction;

    [ObservableProperty]
    private string _overallText = string.Empty;

    [ObservableProperty]
    private int _blockedCount;

    [ObservableProperty]
    private bool _showCompletePrompt;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private bool _isGanttView;

    [ObservableProperty]
    private GanttViewModel? _gantt;

    [ObservableProperty]
    private int? _highlightedSubtaskId;

    public ProjectDetailViewModel(
        Project project,
        IProjectService projects,
        ITaskService tasks,
        ISubtaskEditor editor,
        IProjectExporter exporter,
        bool developerFeatures)
    {
        Model = project;
        _projects = projects;
        _tasks = tasks;
        _editor = editor;
        _exporter = exporter;
        _developerFeatures = developerFeatures;

        Rebuild();
    }

    public Project Model { get; }

    public string Title => Model.TaskItem.Title;

    public string MethodologyBadge => Model.Methodology.ToString();

    public string CategoryName => Model.TaskItem.Category.Name;

    public string CategoryColor => Model.TaskItem.Category.ColorHex;

    public bool IsKanban => Model.Methodology == Methodology.Kanban;

    public bool IsPhased => !IsKanban;

    /// <summary>The Gantt view only makes sense for phased projects (it has phases to plot).</summary>
    public bool GanttAvailable => IsPhased;

    /// <summary>The phased subtask list is shown when we're not in the Gantt view.</summary>
    public bool ShowPhasedList => IsPhased && !IsGanttView;

    public bool IsWaterfall => Model.Methodology == Methodology.Waterfall;

    public bool IsCompleted => Model.TaskItem.IsCompleted;

    /// <summary>Phase sections, in display order. Empty for Kanban.</summary>
    public ObservableCollection<PhaseRowViewModel> Phases { get; } = [];

    // Kanban columns, keyed by the status they represent.
    public ObservableCollection<SubtaskViewModel> TodoColumn { get; } = [];

    public ObservableCollection<SubtaskViewModel> InProgressColumn { get; } = [];

    public ObservableCollection<SubtaskViewModel> ReviewColumn { get; } = [];

    public ObservableCollection<SubtaskViewModel> DoneColumn { get; } = [];

    // ---- rebuilding the view from the model graph ----

    private void Rebuild()
    {
        ProjectRules.RecomputeLocks(Model);

        Phases.Clear();

        if (IsKanban)
        {
            RebuildKanban();
        }
        else
        {
            RebuildPhases();
        }

        RecomputeOverall();
    }

    private void RebuildKanban()
    {
        TodoColumn.Clear();
        InProgressColumn.Clear();
        ReviewColumn.Clear();
        DoneColumn.Clear();

        foreach (var subtask in Model.Subtasks.OrderByDescending(s => s.Priority).ThenBy(s => s.CreatedAt))
        {
            ColumnFor(subtask.Status).Add(Wrap(subtask));
        }
    }

    /// <summary>Blocked cards live in the In Progress lane, flagged red rather than hidden.</summary>
    private ObservableCollection<SubtaskViewModel> ColumnFor(SubtaskStatus status) => status switch
    {
        SubtaskStatus.Done => DoneColumn,
        SubtaskStatus.Review => ReviewColumn,
        SubtaskStatus.InProgress or SubtaskStatus.Blocked => InProgressColumn,
        _ => TodoColumn,
    };

    private void RebuildPhases()
    {
        var orderedPhases = Model.Phases.OrderBy(p => p.Order).ToList();

        if (Model.Methodology == Methodology.Iterative && Model.IterationCount is > 0)
        {
            // One section per (iteration, phase); the phase model is shared, the
            // subtask slice and the label are what differ.
            for (var iteration = 1; iteration <= Model.IterationCount; iteration++)
            {
                foreach (var phase in orderedPhases)
                {
                    var slice = Model.Subtasks.Where(s => s.PhaseId == phase.Id && s.IterationNumber == iteration);
                    Phases.Add(new PhaseRowViewModel(
                        phase,
                        slice.Select(Wrap),
                        displayName: $"Iteration {iteration} · {phase.Name}",
                        iterationNumber: iteration));
                }
            }
        }
        else
        {
            foreach (var phase in orderedPhases)
            {
                var slice = Model.Subtasks.Where(s => s.PhaseId == phase.Id);
                Phases.Add(new PhaseRowViewModel(phase, slice.Select(Wrap)));
            }
        }
    }

    private SubtaskViewModel Wrap(Subtask subtask)
    {
        var vm = new SubtaskViewModel(subtask);
        vm.Changed += OnSubtaskChanged;
        return vm;
    }

    private void RecomputeOverall()
    {
        var progress = Progress.Of(Model.Subtasks);

        OverallPercent = progress.Percent;
        OverallFraction = progress.Fraction;
        BlockedCount = progress.Blocked;
        OverallText = progress.Total == 0
            ? "No subtasks yet — add the first one to start tracking."
            : $"{progress.Done} of {progress.Total} subtasks complete";

        foreach (var row in Phases)
        {
            row.Recompute();
        }

        MaybeOfferCompletion(progress);
    }

    private void MaybeOfferCompletion(Progress progress)
    {
        // Finishing every subtask (which, under Waterfall gating, means the final
        // phase too) is the moment to ask about closing the project out.
        ShowCompletePrompt = !Model.TaskItem.IsCompleted && progress.IsComplete;
    }

    // ---- reacting to changes ----

    private async void OnSubtaskChanged(object? sender, EventArgs e)
    {
        if (sender is SubtaskViewModel vm)
        {
            await _projects.UpdateSubtaskAsync(vm.Model);
            await PersistLocksAndRefreshAsync();
        }
    }

    private async Task PersistLocksAndRefreshAsync()
    {
        var changedLocks = ProjectRules.RecomputeLocks(Model);
        await _projects.UpdatePhaseLocksAsync(changedLocks);
        Rebuild();
    }

    // ---- commands ----

    [RelayCommand]
    private async Task AddToPhaseAsync(PhaseRowViewModel row)
    {
        var subtask = new Subtask
        {
            ProjectId = Model.Id,
            PhaseId = row.Id,
            Status = SubtaskStatus.Todo,
            IterationNumber = Model.Methodology == Methodology.Iterative ? InferIteration(row) : null,
        };

        await CreateSubtaskAsync(subtask);
    }

    [RelayCommand]
    private async Task AddKanbanAsync()
    {
        var subtask = new Subtask { ProjectId = Model.Id, Status = SubtaskStatus.Todo };
        await CreateSubtaskAsync(subtask);
    }

    private static int? InferIteration(PhaseRowViewModel row) => row.IterationNumber ?? 1;

    private async Task CreateSubtaskAsync(Subtask subtask)
    {
        if (!await _editor.EditAsync(subtask, _developerFeatures))
        {
            return;
        }

        await _projects.AddSubtaskAsync(subtask);
        Model.Subtasks.Add(subtask);
        await PersistLocksAndRefreshAsync();
    }

    [RelayCommand]
    private async Task EditSubtaskAsync(SubtaskViewModel vm)
    {
        if (await _editor.EditAsync(vm.Model, _developerFeatures))
        {
            await _projects.UpdateSubtaskAsync(vm.Model);
            await PersistLocksAndRefreshAsync();
        }
    }

    [RelayCommand]
    private async Task DeleteSubtaskAsync(SubtaskViewModel vm)
    {
        await _projects.DeleteSubtaskAsync(vm.Model);
        Model.Subtasks.Remove(vm.Model);
        await PersistLocksAndRefreshAsync();
    }

    /// <summary>
    /// Called from the Kanban board's drag/drop to re-column a card. Setting the status
    /// raises Changed, which persists the move and refreshes the columns.
    /// </summary>
    public void Move(SubtaskViewModel vm, SubtaskStatus status) => vm.MoveTo(status);

    [RelayCommand]
    private async Task MarkProjectCompleteAsync()
    {
        Model.TaskItem.IsCompleted = true;
        Model.TaskItem.CompletedAt = DateTime.Now;
        await _tasks.CompleteAsync(Model.TaskItem);

        ShowCompletePrompt = false;
        OnPropertyChanged(nameof(IsCompleted));
    }

    [RelayCommand]
    private void DismissCompletePrompt() => ShowCompletePrompt = false;

    // Rebuild the timeline each time it's shown so it reflects the latest edits.
    partial void OnIsGanttViewChanged(bool value)
    {
        if (value)
        {
            Gantt = new GanttViewModel(Model, ActivateSubtaskFromGantt);
        }

        OnPropertyChanged(nameof(ShowPhasedList));
    }

    /// <summary>Clicking a subtask bar in the Gantt jumps back to the List and highlights it.</summary>
    private void ActivateSubtaskFromGantt(int subtaskId)
    {
        HighlightedSubtaskId = subtaskId;
        IsGanttView = false;
    }

    partial void OnHighlightedSubtaskIdChanged(int? value)
    {
        foreach (var subtask in Phases.SelectMany(p => p.Subtasks))
        {
            subtask.IsHighlighted = subtask.Id == value;
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = _exporter.PromptForPath(Model);

        if (path is null)
        {
            return;
        }

        // The veil (bound to IsExporting) covers the potentially slow ClosedXML write.
        IsExporting = true;
        try
        {
            await _exporter.WriteAsync(Model, path);
        }
        finally
        {
            IsExporting = false;
        }
    }
}
