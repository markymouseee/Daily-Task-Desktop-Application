using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Models;

namespace DailyTasks.ViewModels;

/// <summary>
/// A single subtask card inside a project. Wraps the <see cref="Subtask"/> model and
/// raises <see cref="Changed"/> whenever the user moves it, so the owning detail view
/// model can persist and re-gate.
/// </summary>
public partial class SubtaskViewModel : ObservableObject
{
    private bool _suppress;

    [ObservableProperty]
    private SubtaskStatus _status;

    /// <summary>Set briefly when jumped to from the Gantt view, to draw attention.</summary>
    [ObservableProperty]
    private bool _isHighlighted;

    public SubtaskViewModel(Subtask model)
    {
        Model = model;
        _status = model.Status;
    }

    /// <summary>Raised after the user changes status, so the detail view persists it.</summary>
    public event EventHandler? Changed;

    public Subtask Model { get; }

    public int Id => Model.Id;

    public string Title => Model.Title;

    public TaskPriority Priority => Model.Priority;

    public DateTime? DueDate => Model.DueDate;

    public double? EstimatedHours => Model.EstimatedHours;

    public double? ActualHours => Model.ActualHours;

    public string? WhyReason => Model.WhyReason;

    public string? ContextResumeNote => Model.ContextResumeNote;

    public string? GitLinkPattern => Model.GitLinkPattern;

    public int? IterationNumber => Model.IterationNumber;

    public bool HasAssignee => Model.AssignedTo is not null;

    public string AssigneeName => Model.AssignedTo?.Name ?? string.Empty;

    public string AssigneeFirstName =>
        AssigneeName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    public string AssigneeColor => Model.AssignedTo?.InitialsColorHex ?? "#64748B";

    public bool IsBlocked => Status == SubtaskStatus.Blocked;

    public bool IsDone => Status == SubtaskStatus.Done;

    public string? BlockedReason => Model.BlockedReason;

    public string StatusLabel => Status switch
    {
        SubtaskStatus.Todo => "To Do",
        SubtaskStatus.InProgress => "In Progress",
        SubtaskStatus.Review => "Review",
        SubtaskStatus.Done => "Done",
        SubtaskStatus.Blocked => "Blocked",
        _ => Status.ToString(),
    };

    public string HoursText => (EstimatedHours, ActualHours) switch
    {
        (null, null) => string.Empty,
        (var est, null) => $"{Trim(est!.Value)}h est",
        (null, var act) => $"{Trim(act!.Value)}h spent",
        var (est, act) => $"{Trim(act!.Value)}/{Trim(est!.Value)}h",
    };

    private static string Trim(double v) => v.ToString("0.#");

    /// <summary>Moves the subtask to a new status (from drag/drop or a menu) and persists it.</summary>
    public void MoveTo(SubtaskStatus status)
    {
        if (Status != status)
        {
            Status = status;
        }
    }

    partial void OnStatusChanged(SubtaskStatus value)
    {
        Model.Status = value;

        // A subtask that is no longer blocked shouldn't keep a stale reason.
        if (value != SubtaskStatus.Blocked)
        {
            Model.BlockedReason = null;
        }

        if (!_suppress)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }

        OnPropertyChanged(nameof(IsBlocked));
        OnPropertyChanged(nameof(IsDone));
        OnPropertyChanged(nameof(StatusLabel));
        OnPropertyChanged(nameof(BlockedReason));
    }

    /// <summary>Re-reads wrapped properties after the model was edited in a dialog.</summary>
    public void Refresh()
    {
        _suppress = true;
        Status = Model.Status;
        _suppress = false;
        OnPropertyChanged(string.Empty);
    }
}
