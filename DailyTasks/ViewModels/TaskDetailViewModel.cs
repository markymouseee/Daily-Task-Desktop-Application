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
    private readonly ITeamCoordinator _teamCoordinator;
    private readonly FocusService _focus;
    private readonly bool _developerFeatures;
    private readonly Action? _openCommits;

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
    private VShapedGanttViewModel? _vGantt;

    [ObservableProperty]
    private CyclicalGanttViewModel? _cycleGantt;

    [ObservableProperty]
    private AgileGanttViewModel? _agileGantt;

    [ObservableProperty]
    private PipelineViewModel? _pipeline;

    [ObservableProperty]
    private int? _highlightedTaskId;

    public TaskDetailViewModel(TaskItem head, ITaskService tasks, ISubtaskEditor editor, IProjectExporter exporter, ITeamCoordinator teamCoordinator, FocusService focus, bool developerFeatures, Action? openCommits = null)
    {
        Model = head;
        _tasks = tasks;
        _editor = editor;
        _exporter = exporter;
        _teamCoordinator = teamCoordinator;
        _focus = focus;
        _developerFeatures = developerFeatures;
        _openCommits = openCommits;

        Rebuild();
    }

    /// <summary>Developer-only tools (the per-project commit feed) show only when enabled.</summary>
    public bool ShowDeveloperTools => _developerFeatures;

    /// <summary>Opens this project's own team (members scoped to it).</summary>
    [RelayCommand]
    private void ManageTeam() => _teamCoordinator.OpenManager(Model.Id, Model.Title);

    /// <summary>Opens this project's git repository / commit feed.</summary>
    [RelayCommand]
    private void OpenCommits() => _openCommits?.Invoke();

    public TaskItem Model { get; }

    public string Title => Model.Title;

    public string MethodologyBadge => Model.Methodology is { } m ? TaskRules.DisplayName(m) : string.Empty;

    public string CategoryName => Model.Category.Name;

    public string CategoryColor => Model.Category.ColorHex;

    /// <summary>The visualization this methodology maps to. Never user-chosen.</summary>
    public ChartType ChartType => Model.Methodology is { } m ? TaskRules.ChartTypeFor(m) : ChartType.SequentialGantt;

    // ---- primary layout family (which "second-column" body the window shows) ----

    /// <summary>Status-column board (Kanban, Lean).</summary>
    public bool UsesBoard => Model.Methodology is Methodology.Kanban or Methodology.Lean;

    /// <summary>Flat, unstructured task list (Big Bang).</summary>
    public bool UsesFlatList => Model.Methodology == Methodology.BigBang;

    /// <summary>
    /// Phase/sprint/stage-organized, so a List ⇄ chart toggle applies. Includes DevOps —
    /// its list edits pipeline stages while the chart is the pipeline diagram (never a Gantt).
    /// </summary>
    public bool UsesPhases => !UsesBoard && !UsesFlatList;

    // ---- Lean WIP ----

    public bool HasWipLimit => Model.Methodology == Methodology.Lean && Model.WipLimit is > 0;

    public int WipLimit => Model.WipLimit ?? 0;

    // ---- toggle + region visibility ----

    /// <summary>Phase/sprint/stage methodologies get a List ⇄ chart toggle.</summary>
    public bool HasChartToggle => UsesPhases;

    /// <summary>Label for the "chart" side of the toggle.</summary>
    public string ChartToggleLabel => Model.Methodology == Methodology.DevOps ? "Pipeline" : "Gantt";

    public bool ShowPhasedList => UsesPhases && !IsGanttView;

    public bool ShowSequentialGantt => IsGanttView && ChartType == ChartType.SequentialGantt;

    public bool ShowVGantt => IsGanttView && ChartType == ChartType.VShapedGantt;

    public bool ShowCyclicalGantt => IsGanttView && ChartType == ChartType.CyclicalGantt;

    public bool ShowAgileGantt => IsGanttView && ChartType == ChartType.AgileGantt;

    public bool ShowPipeline => IsGanttView && Model.Methodology == Methodology.DevOps;

    public bool IsCompleted => Model.IsCompleted;

    public ObservableCollection<PhaseRowViewModel> Phases { get; } = [];

    /// <summary>Big Bang: the head's children as flat recursive cards.</summary>
    public ObservableCollection<TaskItemViewModel> FlatItems { get; } = [];

    public ObservableCollection<TaskItemViewModel> TodoColumn { get; } = [];

    public ObservableCollection<TaskItemViewModel> InProgressColumn { get; } = [];

    public ObservableCollection<TaskItemViewModel> ReviewColumn { get; } = [];

    public ObservableCollection<TaskItemViewModel> DoneColumn { get; } = [];

    /// <summary>Live count in the In Progress column, for the Lean WIP badge.</summary>
    public int InProgressCount => InProgressColumn.Count;

    /// <summary>Lean: true when the In Progress column has exceeded its WIP limit.</summary>
    public bool WipExceeded => HasWipLimit && InProgressColumn.Count > WipLimit;

    public string WipBadge => HasWipLimit ? $"{InProgressColumn.Count}/{WipLimit}" : string.Empty;

    // ---- rebuild ----

    private void Rebuild()
    {
        TaskRules.RecomputeLocks(Model);
        Phases.Clear();

        if (UsesBoard)
        {
            RebuildKanban();
        }
        else if (UsesFlatList)
        {
            RebuildFlat();
        }
        else
        {
            RebuildPhases();
        }

        if (Model.Methodology == Methodology.DevOps)
        {
            Pipeline = new PipelineViewModel(Model);
        }

        RecomputeOverall();
    }

    private void RebuildFlat()
    {
        FlatItems.Clear();

        foreach (var child in Model.Children.OrderByDescending(c => c.Priority).ThenBy(c => c.CreatedAt))
        {
            FlatItems.Add(Wrap(child));
        }
    }

    private void RebuildPhases()
    {
        var ordered = Model.Phases.OrderBy(p => p.Order).ToList();
        var byPhase = Model.Children.ToLookup(c => c.PhaseId);

        if (Model.Methodology is { } m && TaskRules.UsesCycles(m) && Model.IterationCount is > 0)
        {
            for (var iteration = 1; iteration <= Model.IterationCount; iteration++)
            {
                foreach (var phase in ordered)
                {
                    var slice = byPhase[phase.Id].Where(c => c.IterationNumber == iteration).Select(Wrap);
                    Phases.Add(new PhaseRowViewModel(phase, slice, $"Cycle {iteration} · {phase.Name}", iteration));
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

        OnPropertyChanged(nameof(InProgressCount));
        OnPropertyChanged(nameof(WipExceeded));
        OnPropertyChanged(nameof(WipBadge));
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
        if (await _editor.EditAsync(node.Model, Model.Id, _developerFeatures, Model.Methodology == Methodology.XP))
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
            RebuildChart();
        }
    }

    /// <summary>Rebuilds whichever chart the toggle is currently showing.</summary>
    private void RebuildChart()
    {
        switch (ChartType)
        {
            case ChartType.SequentialGantt:
                Gantt = new GanttViewModel(Model, ActivateTaskFromGantt);
                break;
            case ChartType.VShapedGantt:
                VGantt = new VShapedGanttViewModel(Model);
                break;
            case ChartType.CyclicalGantt:
                CycleGantt = new CyclicalGanttViewModel(Model);
                break;
            case ChartType.AgileGantt:
                AgileGantt = new AgileGanttViewModel(Model, ActivateTaskFromGantt, onEdited: task => _ = PersistEditAsync(task));
                break;
        }

        if (Model.Methodology == Methodology.DevOps)
        {
            Pipeline = new PipelineViewModel(Model);
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
            RebuildChart();
        }

        OnPropertyChanged(nameof(ShowPhasedList));
        OnPropertyChanged(nameof(ShowSequentialGantt));
        OnPropertyChanged(nameof(ShowVGantt));
        OnPropertyChanged(nameof(ShowCyclicalGantt));
        OnPropertyChanged(nameof(ShowAgileGantt));
        OnPropertyChanged(nameof(ShowPipeline));
    }

    private void ActivateTaskFromGantt(int taskId)
    {
        HighlightedTaskId = taskId;
        IsGanttView = false;
    }

    /// <summary>Persists an inline Gantt edit and re-syncs the list/board/overall from the model.</summary>
    private async Task PersistEditAsync(TaskItem task)
    {
        await _tasks.UpdateAsync(task);
        Rebuild();
    }

    partial void OnHighlightedTaskIdChanged(int? value)
    {
        foreach (var node in Phases.SelectMany(p => p.Tasks))
        {
            node.IsHighlighted = node.Model.Id == value;
        }
    }
}
