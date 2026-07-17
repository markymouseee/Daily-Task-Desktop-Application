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

    /// <summary>
    /// Manual completion percentage (0–100) typed on the Gantt. Null = derive it from
    /// <see cref="Status"/>. A Done task always reads as 100 regardless.
    /// </summary>
    public int? ProgressPercent { get; set; }

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

    /// <summary>
    /// Planned cycle/sprint count. Drives the number of iterations for the cyclical
    /// methodologies (Spiral, Iterative &amp; Incremental, RAD) and the number of sprint
    /// swimlanes for the agile methodologies (Agile, Scrum, XP).
    /// </summary>
    public int? IterationCount { get; set; }

    /// <summary>
    /// Sprint length in days for the agile methodologies (default 14). Sets each sprint
    /// swimlane's date-range header. Null for non-agile heads.
    /// </summary>
    public int? SprintLengthDays { get; set; }

    /// <summary>
    /// Work-in-progress limit for the In Progress column, used only by Lean. The column
    /// shows an "n/limit" badge and warns when the count exceeds it. Null = no limit.
    /// </summary>
    public int? WipLimit { get; set; }

    /// <summary>
    /// XP practice tags for a subtask under an XP head (Pair Programming, Test-Driven,
    /// Code Review), rendered as icon badges. <see cref="XpPractice.None"/> otherwise.
    /// </summary>
    public XpPractice XpPractices { get; set; } = XpPractice.None;

    /// <summary>The phases owned by this task when it's methodology-organized.</summary>
    public ICollection<Phase> Phases { get; } = [];

    /// <summary>The phase this task sits in (children of a phased parent).</summary>
    public int? PhaseId { get; set; }

    public Phase? Phase { get; set; }

    /// <summary>Which iteration this belongs to; only set under an Iterative parent.</summary>
    public int? IterationNumber { get; set; }

    // ---- assignee ----

    /// <summary>The "primary" assignee (first of <see cref="Assignees"/>). Kept for the simpler
    /// single-avatar views; multi-assignment lives in <see cref="Assignees"/>.</summary>
    public int? AssignedToId { get; set; }

    public TeamMember? AssignedTo { get; set; }

    /// <summary>Everyone this activity is assigned to (many-to-many with the project team).</summary>
    public ICollection<TeamMember> Assignees { get; } = [];

    /// <summary>When this task is a project head, the members that belong to its team
    /// (inverse of <see cref="TeamMember.OwnerProject"/>) — used to collapse "all assigned" to
    /// "Team".</summary>
    public ICollection<TeamMember> ProjectTeam { get; } = [];

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

    /// <summary>
    /// Local git repository this project tracks. The commit feed lists its commits and the
    /// watcher scans it to auto-complete the project's linked subtasks. Only meaningful on a
    /// methodology-organized head; null otherwise.
    /// </summary>
    public string? GitRepoPath { get; set; }

    /// <summary>When set, completing the task spawns the next occurrence.</summary>
    public RecurrenceKind Recurrence { get; set; } = RecurrenceKind.None;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }
}
