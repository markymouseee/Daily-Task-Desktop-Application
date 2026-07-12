namespace DailyTasks.Models;

/// <summary>
/// The single task entity. Every task-like thing is one of these: a plain to-do, a parent
/// with children, or a methodology-organized project head. Children hang off
/// <see cref="ParentTaskId"/> to any depth; <see cref="Methodology"/> (when set) unlocks
/// phases, the Gantt view and Excel export for that subtree.
/// </summary>
public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    // ---- hierarchy ----

    /// <summary>Parent task, or null for a top-level task. Self-referencing to any depth.</summary>
    public int? ParentTaskId { get; set; }

    public TaskItem? Parent { get; set; }

    public ICollection<TaskItem> Children { get; } = [];

    // ---- scheduling / effort ----

    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public double? EstimatedHours { get; set; }

    public double? ActualHours { get; set; }

    // ---- status ----

    /// <summary>
    /// Canonical completion/workflow state. <see cref="IsCompleted"/> is derived from it,
    /// so the checkbox and the richer Kanban/Gantt statuses stay in one place.
    /// </summary>
    public WorkStatus Status { get; set; } = WorkStatus.Todo;

    /// <summary>Not mapped — a convenience over <see cref="Status"/>.</summary>
    public bool IsCompleted
    {
        get => Status == WorkStatus.Done;
        set => Status = value ? WorkStatus.Done : WorkStatus.Todo;
    }

    /// <summary>Why the work stalled; shown on the Blocked badge. Null unless blocked.</summary>
    public string? BlockedReason { get; set; }

    // ---- methodology / phases (only meaningful once a task has children) ----

    /// <summary>Null = a plain (unstructured) task; set = organized as an SDLC methodology.</summary>
    public Methodology? Methodology { get; set; }

    /// <summary>Custom phase names, used only when <see cref="Methodology"/> is Custom.</summary>
    public List<string> CustomPhases { get; set; } = [];

    /// <summary>Planned iteration count, used only for Iterative.</summary>
    public int? IterationCount { get; set; }

    /// <summary>The phases owned by this task when it's methodology-organized.</summary>
    public ICollection<Phase> Phases { get; } = [];

    /// <summary>The phase this task sits in (children of a phased parent).</summary>
    public int? PhaseId { get; set; }

    public Phase? Phase { get; set; }

    /// <summary>Which iteration this belongs to; only set under an Iterative parent.</summary>
    public int? IterationNumber { get; set; }

    // ---- assignee ----

    public int? AssignedToId { get; set; }

    public TeamMember? AssignedTo { get; set; }

    // ---- personal-task features (top-level tasks in practice) ----

    /// <summary>Incremented each time the task is rescheduled (stale-task nudge).</summary>
    public int PostponedCount { get; set; }

    /// <summary>Why this task matters, e.g. "so I don't get charged a late fee".</summary>
    public string? WhyReason { get; set; }

    /// <summary>Where you left off, shown when the task is reopened.</summary>
    public string? ContextResumeNote { get; set; }

    /// <summary>The day this task was pinned as one of the "Big 3".</summary>
    public DateTime? BigThreeDate { get; set; }

    /// <summary>When the stale-task nudge last asked about this task.</summary>
    public DateTime? LastNudgedAt { get; set; }

    /// <summary>A branch name or commit tag; the git watcher auto-completes on a match.</summary>
    public string? GitLink { get; set; }

    /// <summary>When set, completing the task spawns the next occurrence.</summary>
    public RecurrenceKind Recurrence { get; set; } = RecurrenceKind.None;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }
}
