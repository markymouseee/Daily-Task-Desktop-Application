using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// Drives the methodology detail window for one organized task head. Its child tasks are the
/// "subtasks"; they're grouped into phase sections (or a Kanban board), kept gated for
/// Waterfall, and mirrored in the Gantt. Implements <see cref="ITaskCardHost"/> so the reused
/// recursive task cards route their actions back here.
/// </summary>
public partial class TaskDetailViewModel : ObservableObject, ITaskCardHost
{
    private readonly ITaskService _tasks;
    private readonly ISubtaskEditor _editor;
    private readonly IProjectExporter _exporter;
    private readonly FocusService _focus;
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
    private int? _highlightedTaskId;

    public TaskDetailViewModel(TaskItem head, ITaskService tasks, ISubtaskEditor editor, IProjectExporter exporter, FocusService focus, bool developerFeatures)
    {
        Model = head;
        _tasks = tasks;
        _editor = editor;
        _exporter = exporter;
        _focus = focus;
        _developerFeatures = developerFeatures;

        Rebuild();
    }

    public TaskItem Model { get; }

    public string Title => Model.Title;

    public string MethodologyBadge => Model.Methodology?.ToString() ?? string.Empty;

    public string CategoryName => Model.Category.Name;

    public string CategoryColor => Model.Category.ColorHex;

    public bool IsKanban => Model.Methodology == Methodology.Kanban;

    public bool IsPhased => !IsKanban;

    public bool GanttAvailable => IsPhased;

    public bool ShowPhasedList => IsPhased && !IsGanttView;

    public bool IsCompleted => Model.IsCompleted;

    public ObservableCollection<PhaseRowViewModel> Phases { get; } = [];

    public ObservableCollection<TaskItemViewModel> TodoColumn { get; } = [];

    public ObservableCollection<TaskItemViewModel> InProgressColumn { get; } = [];

    public ObservableCollection<TaskItemViewModel> ReviewColumn { get; } = [];

    public ObservableCollection<TaskItemViewModel> DoneColumn { get; } = [];

    // ---- rebuild ----

    private void Rebuild()
    {
        TaskRules.RecomputeLocks(Model);
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

    private void RebuildPhases()
    {
        var ordered = Model.Phases.OrderBy(p => p.Order).ToList();
        var byPhase = Model.Children.ToLookup(c => c.PhaseId);

        if (Model.Methodology == Methodology.Iterative && Model.IterationCount is > 0)
        {
            for (var iteration = 1; iteration <= Model.IterationCount; iteration++)
            {
                foreach (var phase in ordered)
                {
                    var slice = byPhase[phase.Id].Where(c => c.IterationNumber == iteration).Select(Wrap);
                    Phases.Add(new PhaseRowViewModel(phase, slice, $"Iteration {iteration} · {phase.Name}", iteration));
                }
            }
        }
        else
        {
            foreach (var phase in ordered)
            {
                Phases.Add(new PhaseRowViewModel(phase, byPhase[phase.Id].Select(Wrap)));
            }
        }
    }

    private void RebuildKanban()
    {
        TodoColumn.Clear();
        InProgressColumn.Clear();
        ReviewColumn.Clear();
        DoneColumn.Clear();

        foreach (var child in Model.Children.OrderByDescending(c => c.Priority).ThenBy(c => c.CreatedAt))
        {
            ColumnFor(child.Status).Add(Wrap(child));
        }
    }

    private ObservableCollection<TaskItemViewModel> ColumnFor(WorkStatus status) => status switch
    {
        WorkStatus.Done => DoneColumn,
        WorkStatus.Review => ReviewColumn,
        WorkStatus.InProgress or WorkStatus.Blocked => InProgressColumn,
        _ => TodoColumn,
    };

    private TaskItemViewModel Wrap(TaskItem child) => new(child, this);

    private void RecomputeOverall()
    {
        var progress = Progress.Of(Model.Children);

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

        ShowCompletePrompt = !Model.IsCompleted && progress.IsComplete;
    }

    // ---- ITaskCardHost (reused recursive cards route here) ----

    public Task StartFocusAsync(TaskItemViewModel node) => _focus.StartAsync(node.Model);

    public async Task EditAsync(TaskItemViewModel node)
    {
        if (await _editor.EditAsync(node.Model, _developerFeatures))
        {
            await _tasks.UpdateAsync(node.Model);
            await PersistLocksAndRefreshAsync();
        }
    }

    public async Task DeleteAsync(TaskItemViewModel node)
    {
        await _tasks.DeleteAsync(node.Model);
        RemoveFromModel(node.Model);
        await PersistLocksAndRefreshAsync();
    }

    public async Task AddChildAsync(TaskItemViewModel node, string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var child = new TaskItem
        {
            Title = title.Trim(),
            CategoryId = node.Model.CategoryId,
            Category = node.Model.Category,
            ParentTaskId = node.Model.Id,
        };

        await _tasks.AddAsync(child);
        node.Model.Children.Add(child);
        await PersistLocksAndRefreshAsync();
    }

    public Task OrganizeAsync(TaskItemViewModel node) => Task.CompletedTask;

    public Task OpenDetailAsync(TaskItemViewModel node) => Task.CompletedTask;

    public async Task ChangedAsync(TaskItemViewModel node)
    {
        await _tasks.UpdateAsync(node.Model);
        await PersistLocksAndRefreshAsync();
    }

    private void RemoveFromModel(TaskItem task)
    {
        if (task.ParentTaskId == Model.Id)
        {
            Model.Children.Remove(task);
        }
        else
        {
            foreach (var descendant in TaskTree.Descendants(Model))
            {
                if (descendant.Children.Remove(task))
                {
                    break;
                }
            }
        }
    }

    private async Task PersistLocksAndRefreshAsync()
    {
        var changed = TaskRules.RecomputeLocks(Model);
        await _tasks.UpdatePhaseLocksAsync(changed);
        Rebuild();

        if (IsGanttView)
        {
            Gantt = new GanttViewModel(Model, ActivateTaskFromGantt);
        }
    }

    // ---- commands ----

    [RelayCommand]
    private async Task AddToPhaseAsync(PhaseRowViewModel row)
    {
        var title = row.NewTaskTitle.Trim();
        if (title.Length == 0)
        {
            return;
        }

        row.NewTaskTitle = string.Empty;

        var child = new TaskItem
        {
            Title = title,
            CategoryId = Model.CategoryId,
            Category = Model.Category,
            ParentTaskId = Model.Id,
            PhaseId = row.Id,
            IterationNumber = row.IterationNumber,
        };

        await _tasks.AddAsync(child);
        Model.Children.Add(child);
        await PersistLocksAndRefreshAsync();
    }

    [RelayCommand]
    private async Task AddKanbanAsync()
    {
        var child = new TaskItem
        {
            Title = "New task",
            CategoryId = Model.CategoryId,
            Category = Model.Category,
            ParentTaskId = Model.Id,
        };

        await _tasks.AddAsync(child);
        Model.Children.Add(child);
        Rebuild();
    }

    public void Move(TaskItemViewModel node, WorkStatus status) => node.MoveTo(status);

    [RelayCommand]
    private async Task MarkProjectCompleteAsync()
    {
        Model.IsCompleted = true;
        Model.CompletedAt = DateTime.Now;
        await _tasks.CompleteAsync(Model);

        ShowCompletePrompt = false;
        OnPropertyChanged(nameof(IsCompleted));
    }

    [RelayCommand]
    private void DismissCompletePrompt() => ShowCompletePrompt = false;

    [RelayCommand]
    private async Task ExportAsync()
    {
        var path = _exporter.PromptForPath(Model);
        if (path is null)
        {
            return;
        }

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

    partial void OnIsGanttViewChanged(bool value)
    {
        if (value)
        {
            Gantt = new GanttViewModel(Model, ActivateTaskFromGantt);
        }

        OnPropertyChanged(nameof(ShowPhasedList));
    }

    private void ActivateTaskFromGantt(int taskId)
    {
        HighlightedTaskId = taskId;
        IsGanttView = false;
    }

    partial void OnHighlightedTaskIdChanged(int? value)
    {
        foreach (var node in Phases.SelectMany(p => p.Tasks))
        {
            node.IsHighlighted = node.Model.Id == value;
        }
    }
}
