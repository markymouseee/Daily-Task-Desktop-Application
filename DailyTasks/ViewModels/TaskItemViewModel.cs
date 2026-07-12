using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// One task card — recursive. Wraps a <see cref="TaskItem"/> and its children; a node with
/// children shows a rollup progress bar and an expand chevron, and (once organized with a
/// methodology) becomes Gantt/Excel-eligible. Commands route to the owning list via
/// <see cref="ITaskCardHost"/>.
/// </summary>
public partial class TaskItemViewModel : ObservableObject
{
    private const int MaxIndentDepth = 5;

    private readonly ITaskCardHost _host;
    private bool _suppress;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private WorkStatus _status;

    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>Briefly set when jumped to from the Gantt view.</summary>
    [ObservableProperty]
    private bool _isHighlighted;

    [ObservableProperty]
    private string _newChildTitle = string.Empty;

    public TaskItemViewModel(TaskItem model, ITaskCardHost host, int depth = 0)
    {
        Model = model;
        _host = host;
        Depth = depth;
        _isCompleted = model.IsCompleted;
        _status = model.Status;

        foreach (var child in model.Children.OrderByDescending(c => c.Priority).ThenBy(c => c.CreatedAt))
        {
            Children.Add(new TaskItemViewModel(child, host, depth + 1) { ParentNode = this });
        }
    }

    public TaskItem Model { get; }

    public int Depth { get; }

    /// <summary>The parent card, or null at the top level. Used to bubble rollup refreshes.</summary>
    public TaskItemViewModel? ParentNode { get; set; }

    public ObservableCollection<TaskItemViewModel> Children { get; } = [];

    // ---- basics ----

    public string Title => Model.Title;

    public string? WhyReason => Model.WhyReason;

    public string? ContextResumeNote => Model.ContextResumeNote;

    public string? GitLink => Model.GitLink;

    public string CategoryName => Model.Category.Name;

    public string CategoryColor => Model.Category.ColorHex;

    public TaskPriority Priority => Model.Priority;

    public DateTime? DueDate => Model.DueDate;

    public DateTime? CompletedAt => Model.CompletedAt;

    public DateTime DueSortKey => Model.DueDate ?? DateTime.MaxValue;

    public RecurrenceKind Recurrence => Model.Recurrence;

    public bool IsRecurring => Model.Recurrence != RecurrenceKind.None;

    public string RecurrenceLabel => Model.Recurrence switch
    {
        RecurrenceKind.Daily => "Daily",
        RecurrenceKind.Weekly => "Weekly",
        RecurrenceKind.Monthly => "Monthly",
        _ => string.Empty,
    };

    // ---- hierarchy / indent ----

    public bool HasChildren => Children.Count > 0;

    public double IndentPixels => Math.Min(Depth, MaxIndentDepth) * 18;

    // ---- rollup (over all descendants) ----

    public Progress Rollup => Progress.Of(TaskTree.Descendants(Model));

    public bool ShowProgress => HasChildren;

    public double ProgressFraction => Rollup.Fraction;

    public int PercentComplete => Rollup.Percent;

    public string ProgressText => $"{Rollup.Done}/{Rollup.Total} done";

    public bool HasBlockedDescendants => Rollup.Blocked > 0;

    // ---- methodology ----

    public Methodology? Methodology => Model.Methodology;

    public bool IsOrganized => Model.Methodology is not null;

    public string MethodologyBadge => Model.Methodology?.ToString() ?? string.Empty;

    /// <summary>Organize-as only makes sense once a task has children to structure.</summary>
    public bool CanOrganize => HasChildren;

    // ---- status / blocked (used in the detail / Kanban view) ----

    public string StatusLabel => Status switch
    {
        WorkStatus.Todo => "To Do",
        WorkStatus.InProgress => "In Progress",
        WorkStatus.Review => "Review",
        WorkStatus.Done => "Done",
        WorkStatus.Blocked => "Blocked",
        _ => Status.ToString(),
    };

    public bool IsBlocked => Status == WorkStatus.Blocked;

    public bool IsDone => Status == WorkStatus.Done;

    public string? BlockedReason => Model.BlockedReason;

    public string HoursText => (Model.EstimatedHours, Model.ActualHours) switch
    {
        (null, null) => string.Empty,
        (var est, null) => $"{Trim(est!.Value)}h est",
        (null, var act) => $"{Trim(act!.Value)}h spent",
        var (est, act) => $"{Trim(act!.Value)}/{Trim(est!.Value)}h",
    };

    private static string Trim(double v) => v.ToString("0.#");

    // ---- assignee ----

    public bool HasAssignee => Model.AssignedTo is not null;

    public string AssigneeName => Model.AssignedTo?.Name ?? string.Empty;

    public string AssigneeFirstName =>
        AssigneeName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    public string AssigneeColor => Model.AssignedTo?.InitialsColorHex ?? "#64748B";

    // ---- commands ----

    [RelayCommand]
    private Task Edit() => _host.EditAsync(this);

    [RelayCommand]
    private Task Delete() => _host.DeleteAsync(this);

    [RelayCommand]
    private Task StartFocus() => _host.StartFocusAsync(this);

    [RelayCommand]
    private Task Organize() => _host.OrganizeAsync(this);

    [RelayCommand]
    private Task OpenDetail() => _host.OpenDetailAsync(this);

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;

    [RelayCommand(CanExecute = nameof(CanAddChild))]
    private async Task AddChild()
    {
        var title = NewChildTitle.Trim();
        NewChildTitle = string.Empty;
        await _host.AddChildAsync(this, title);
    }

    private bool CanAddChild() => !string.IsNullOrWhiteSpace(NewChildTitle);

    partial void OnNewChildTitleChanged(string value) => AddChildCommand.NotifyCanExecuteChanged();

    // ---- completion / status ----

    public event EventHandler? RollupChanged;

    public void MoveTo(WorkStatus status)
    {
        if (Status != status)
        {
            Status = status;
        }
    }

    partial void OnIsCompletedChanged(bool value)
    {
        if (_suppress)
        {
            return;
        }

        Model.CompletedAt = value ? DateTime.Now : null;
        _suppress = true;
        Status = value ? WorkStatus.Done : WorkStatus.Todo;
        _suppress = false;

        Model.Status = Status;
        _ = _host.ChangedAsync(this);
    }

    partial void OnStatusChanged(WorkStatus value)
    {
        Model.Status = value;
        if (value != WorkStatus.Blocked)
        {
            Model.BlockedReason = null;
        }

        _suppress = true;
        IsCompleted = value == WorkStatus.Done;
        _suppress = false;

        OnPropertyChanged(nameof(IsBlocked));
        OnPropertyChanged(nameof(IsDone));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(BlockedReason));

        if (!_suppress)
        {
            _ = _host.ChangedAsync(this);
        }
    }

    /// <summary>Re-reads wrapped state after an edit or a child change; refreshes rollups up the tree.</summary>
    public void Refresh()
    {
        _suppress = true;
        IsCompleted = Model.IsCompleted;
        Status = Model.Status;
        _suppress = false;
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Recomputes the rollup-derived properties (after a descendant changed).</summary>
    public void RefreshRollup()
    {
        OnPropertyChanged(nameof(Rollup));
        OnPropertyChanged(nameof(ProgressFraction));
        OnPropertyChanged(nameof(PercentComplete));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(ShowProgress));
        OnPropertyChanged(nameof(HasBlockedDescendants));
        RollupChanged?.Invoke(this, EventArgs.Empty);
    }
}
