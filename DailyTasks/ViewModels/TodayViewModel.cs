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

    private readonly ICategoryService _categories;
    private readonly SettingsService _settings;

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

    public TodayViewModel(ITaskService tasks, ICategoryService categories, SettingsService settings)
        : base(tasks)
    {
        _categories = categories;
        _settings = settings;

        tasks.TaskAdded += OnTaskAdded;
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
    /// Overdue tasks stay visible rather than silently disappearing.
    /// </summary>
    protected override bool Includes(TaskItem task) =>
        !task.IsCompleted && (task.DueDate is null || task.DueDate.Value.Date <= DateTime.Today);

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
    }

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
        IsEmpty = Items.Count == 0 && BigThree.Count == 0;
        HasBigThree = BigThree.Count > 0;
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

    // ---- the Big 3 ritual ----

    protected override void AfterLoad()
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
}
