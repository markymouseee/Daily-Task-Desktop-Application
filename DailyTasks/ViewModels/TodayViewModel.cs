using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// Registered as a singleton so quick-capture additions land on an already-open
/// Today page, and so the service subscription below has a single owner.
/// </summary>
public partial class TodayViewModel : TaskListViewModel
{
    private const int MaxBigThree = 3;
    private const int StaleAfterDays = 5;

    private readonly ICategoryService _categories;
    private readonly SettingsService _settings;
    private readonly IProjectService _projectService;
    private readonly IProjectCoordinator _projectCoordinator;
    private readonly ITeamCoordinator _teamCoordinator;
    private readonly Queue<TaskItem> _staleQueue = new();

    private TaskItem? _staleTask;

    /// <summary>The typed text with date/priority words removed; what actually gets saved.</summary>
    private string _parsedTitle = string.Empty;

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private Category? _newTaskCategory;

    [ObservableProperty]
    private TaskPriority _newTaskPriority = TaskPriority.Medium;

    [ObservableProperty]
    private DateTime? _newTaskDueDate = DateTime.Today;

    [ObservableProperty]
    private string _parsePreview = string.Empty;

    [ObservableProperty]
    private bool _hasBigThree;

    [ObservableProperty]
    private bool _showBigThreePrompt;

    [ObservableProperty]
    private string _bigThreePromptHint = string.Empty;

    [ObservableProperty]
    private bool _showStaleNudge;

    [ObservableProperty]
    private string _staleText = string.Empty;

    [ObservableProperty]
    private string _staleMoreText = string.Empty;

    [ObservableProperty]
    private double _freeHoursToday;

    [ObservableProperty]
    private bool _showWorkloadWarning;

    [ObservableProperty]
    private string _workloadText = string.Empty;

    [ObservableProperty]
    private bool _hasProjects;

    public TodayViewModel(
        ITaskService tasks,
        ICategoryService categories,
        SettingsService settings,
        FocusService focus,
        ITaskEditor editor,
        GitWatcherService gitWatcher,
        IProjectService projectService,
        IProjectCoordinator projectCoordinator,
        ITeamCoordinator teamCoordinator)
        : base(tasks, focus, editor)
    {
        _categories = categories;
        _settings = settings;
        _projectService = projectService;
        _projectCoordinator = projectCoordinator;
        _teamCoordinator = teamCoordinator;
        _freeHoursToday = settings.FreeHoursPerDay;

        tasks.TaskAdded += OnTaskAdded;
        gitWatcher.TaskCompleted += OnTaskCompletedByCommit;
    }

    /// <summary>Active projects surfaced as distinct cards, above the flat task list.</summary>
    public ObservableCollection<ProjectViewModel> TodayProjects { get; } = [];

    /// <summary>
    /// The watcher works on its own task instances, so match by id, sync our copy,
    /// and let the card leave the page.
    /// </summary>
    private void OnTaskCompletedByCommit(object? sender, TaskItem task)
    {
        var card = Items.Concat(BigThree).FirstOrDefault(i => i.Model.Id == task.Id);

        if (card is null)
        {
            return;
        }

        card.Model.IsCompleted = true;
        card.Model.CompletedAt = task.CompletedAt;
        RemoveItem(card);
    }

    public override string EmptyMessage => "Nothing due today. Add a task above to get started.";

    public ObservableCollection<Category> Categories { get; } = [];

    /// <summary>Today's pinned tasks, shown above everything else.</summary>
    public ObservableCollection<TaskItemViewModel> BigThree { get; } = [];

    public ObservableCollection<BigThreeCandidate> BigThreeCandidates { get; } = [];

    public IReadOnlyList<TaskPriority> Priorities { get; } =
        [TaskPriority.High, TaskPriority.Medium, TaskPriority.Low];

    /// <summary>
    /// Today means: not done, and either due on/before today or not scheduled at all.
    /// Overdue tasks stay visible rather than silently disappearing. Project heads are
    /// excluded here — they render as their own cards from <see cref="TodayProjects"/>.
    /// </summary>
    protected override bool Includes(TaskItem task) =>
        task.TaskType == TaskType.Simple
        && !task.IsCompleted
        && (task.DueDate is null || task.DueDate.Value.Date <= DateTime.Today);

    private static bool IsTodayProject(Project project) =>
        !project.TaskItem.IsCompleted
        && (project.TaskItem.DueDate is null || project.TaskItem.DueDate.Value.Date <= DateTime.Today);

    private static bool IsPinnedToday(TaskItem task) => task.BigThreeDate?.Date == DateTime.Today;

    public override async Task LoadAsync()
    {
        if (Categories.Count == 0)
        {
            foreach (var category in await _categories.GetAllAsync())
            {
                Categories.Add(category);
            }

            NewTaskCategory ??= Categories.FirstOrDefault();
        }

        await base.LoadAsync();
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        TodayProjects.Clear();

        foreach (var project in await _projectService.GetAllAsync())
        {
            if (IsTodayProject(project))
            {
                TodayProjects.Add(new ProjectViewModel(project));
            }
        }

        HasProjects = TodayProjects.Count > 0;
        UpdateEmpty();
    }

    [RelayCommand]
    private async Task OpenProjectAsync(ProjectViewModel project)
    {
        await _projectCoordinator.OpenDetailAsync(project.Id);

        // The project may have advanced or completed while the detail was open.
        await LoadProjectsAsync();
    }

    // ---- overflow (⋯) menu ----

    [RelayCommand]
    private async Task NewProjectFromMenuAsync()
    {
        if (await _projectCoordinator.CreateAsync() is not null)
        {
            await LoadProjectsAsync();
        }
    }

    [RelayCommand]
    private void ManageTeam() => _teamCoordinator.OpenManager();

    // ---- two-collection bookkeeping: pinned tasks live in BigThree, the rest in Items ----

    protected override void ClearAll()
    {
        foreach (var item in BigThree)
        {
            Detach(item);
        }

        BigThree.Clear();
        base.ClearAll();
    }

    protected override void AddItem(TaskItemViewModel item)
    {
        if (!IsPinnedToday(item.Model))
        {
            base.AddItem(item);
            return;
        }

        Attach(item);
        BigThree.Add(item);
        UpdateEmpty();
    }

    protected override void RemoveItem(TaskItemViewModel item)
    {
        if (!BigThree.Contains(item))
        {
            base.RemoveItem(item);
            return;
        }

        Detach(item);
        BigThree.Remove(item);
        UpdateEmpty();
    }

    protected override void UpdateEmpty()
    {
        IsEmpty = Items.Count == 0 && BigThree.Count == 0 && TodayProjects.Count == 0;
        HasBigThree = BigThree.Count > 0;
        RecomputeWorkload();
    }

    // ---- task/time mismatch warning ----

    partial void OnFreeHoursTodayChanged(double value)
    {
        _settings.FreeHoursPerDay = value;
        RecomputeWorkload();
    }

    private void RecomputeWorkload()
    {
        var plannedMinutes = Items.Concat(BigThree).Sum(i => i.Model.EstimatedMinutes ?? 0);
        var freeMinutes = FreeHoursToday * 60;

        ShowWorkloadWarning = plannedMinutes > 0 && plannedMinutes > freeMinutes;

        if (ShowWorkloadWarning)
        {
            WorkloadText =
                $"Today's estimates add up to {FormatMinutes(plannedMinutes)}, "
                + $"but you have {FormatMinutes((int)freeMinutes)} free. Something may need to move.";
        }
    }

    private static string FormatMinutes(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;

        return (hours, mins) switch
        {
            (0, _) => $"{mins}m",
            (_, 0) => $"{hours}h",
            _ => $"{hours}h {mins}m",
        };
    }

    // ---- quick add ----

    [RelayCommand(CanExecute = nameof(CanAddTask))]
    private async Task AddTaskAsync()
    {
        var category = NewTaskCategory!;

        var task = new TaskItem
        {
            Title = _parsedTitle,
            CategoryId = category.Id,
            Category = category,
            Priority = NewTaskPriority,
            DueDate = NewTaskDueDate,
        };

        // The card is added by OnTaskAdded, which also covers the quick-capture bar.
        await Tasks.AddAsync(task);

        NewTaskTitle = string.Empty;
    }

    private bool CanAddTask() => _parsedTitle.Length > 0 && NewTaskCategory is not null;

    partial void OnNewTaskTitleChanged(string value)
    {
        var parsed = TaskTextParser.Parse(value);

        _parsedTitle = parsed.Title;
        ParsePreview = parsed.HasHints ? $"{parsed.Title} — {parsed.Summary}" : string.Empty;

        // Only overwrite the pickers when the sentence actually said something.
        if (parsed.DueDate is { } due)
        {
            NewTaskDueDate = due;
        }

        if (parsed.Priority is { } priority)
        {
            NewTaskPriority = priority;
        }

        AddTaskCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewTaskCategoryChanged(Category? value) => AddTaskCommand.NotifyCanExecuteChanged();

    private void OnTaskAdded(object? sender, TaskItem task)
    {
        if (Includes(task))
        {
            AddItem(new TaskItemViewModel(task));
        }
    }

    // ---- once-a-day rituals ----

    protected override Task AfterLoadAsync(IReadOnlyList<TaskItem> allTasks)
    {
        OfferBigThree();
        QueueStaleTasks(allTasks);
        return Task.CompletedTask;
    }

    // ---- the Big 3 ritual ----

    private void OfferBigThree()
    {
        var alreadyAnswered = _settings.LastBigThreePrompt?.Date == DateTime.Today;

        if (alreadyAnswered || HasBigThree || Items.Count == 0)
        {
            return;
        }

        BigThreeCandidates.Clear();

        foreach (var item in Items)
        {
            BigThreeCandidates.Add(new BigThreeCandidate(item, OnCandidateSelectionChanged));
        }

        UpdatePromptHint();
        ShowBigThreePrompt = true;
    }

    private int SelectedCandidateCount => BigThreeCandidates.Count(c => c.IsSelected);

    private void OnCandidateSelectionChanged()
    {
        UpdatePromptHint();
        ConfirmBigThreeCommand.NotifyCanExecuteChanged();
    }

    private void UpdatePromptHint()
    {
        var selected = SelectedCandidateCount;

        BigThreePromptHint = selected switch
        {
            0 => "Choose up to 3.",
            > MaxBigThree => $"{selected} selected — that's more than 3.",
            _ => $"{selected} of {MaxBigThree} selected.",
        };
    }

    [RelayCommand(CanExecute = nameof(CanConfirmBigThree))]
    private async Task ConfirmBigThreeAsync()
    {
        foreach (var candidate in BigThreeCandidates.Where(c => c.IsSelected).ToList())
        {
            candidate.Task.Model.BigThreeDate = DateTime.Today;
            await Tasks.UpdateAsync(candidate.Task.Model);

            // Move between collections; the completion handler stays attached.
            Items.Remove(candidate.Task);
            BigThree.Add(candidate.Task);
        }

        UpdateEmpty();
        DismissPrompt();
    }

    private bool CanConfirmBigThree() => SelectedCandidateCount is >= 1 and <= MaxBigThree;

    [RelayCommand]
    private void SkipBigThree() => DismissPrompt();

    private void DismissPrompt()
    {
        _settings.LastBigThreePrompt = DateTime.Today;
        ShowBigThreePrompt = false;
        BigThreeCandidates.Clear();
    }

    // ---- the stale-task nudge ----

    private void QueueStaleTasks(IReadOnlyList<TaskItem> allTasks)
    {
        _staleQueue.Clear();

        foreach (var task in allTasks.Where(IsStale).OrderBy(t => t.CreatedAt))
        {
            _staleQueue.Enqueue(task);
        }

        ShowNextStale();
    }

    private static bool IsStale(TaskItem task) =>
        !task.IsCompleted
        && (DateTime.Today - task.CreatedAt.Date).Days >= StaleAfterDays
        && (task.DueDate is null || task.DueDate.Value.Date < DateTime.Today)
        && task.LastNudgedAt?.Date != DateTime.Today
        && !IsPinnedToday(task);

    private void ShowNextStale()
    {
        if (!_staleQueue.TryDequeue(out _staleTask))
        {
            ShowStaleNudge = false;
            return;
        }

        var age = (DateTime.Today - _staleTask.CreatedAt.Date).Days;

        StaleText = $"“{_staleTask.Title}” has been open {age} days. Still relevant?";
        StaleMoreText = _staleQueue.Count > 0 ? $"+{_staleQueue.Count} more to review" : string.Empty;
        ShowStaleNudge = true;
    }

    [RelayCommand]
    private async Task StaleDoNowAsync()
    {
        var task = _staleTask!;
        task.LastNudgedAt = DateTime.Now;
        await Tasks.UpdateAsync(task);
        await Focus.StartAsync(task);
        ShowNextStale();
    }

    [RelayCommand]
    private async Task StaleRescheduleAsync()
    {
        var task = _staleTask!;
        task.DueDate = DateTime.Today.AddDays(1);
        task.PostponedCount++;
        task.LastNudgedAt = DateTime.Now;
        await Tasks.UpdateAsync(task);

        // Now due tomorrow, so it leaves the Today list.
        RemoveCardFor(task);
        ShowNextStale();
    }

    [RelayCommand]
    private async Task StaleDeleteAsync()
    {
        var task = _staleTask!;
        await Tasks.DeleteAsync(task);
        RemoveCardFor(task);
        ShowNextStale();
    }

    [RelayCommand]
    private async Task StaleSkipAsync()
    {
        var task = _staleTask!;
        task.LastNudgedAt = DateTime.Now;
        await Tasks.UpdateAsync(task);
        ShowNextStale();
    }

    private void RemoveCardFor(TaskItem task)
    {
        var card = Items.Concat(BigThree).FirstOrDefault(i => ReferenceEquals(i.Model, task));

        if (card is not null)
        {
            RemoveItem(card);
        }
    }
}
