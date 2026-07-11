namespace DailyTasks.Models;

/// <summary>
/// A single unit of work inside a project. Belongs to a project and, for phased
/// methodologies, to a <see cref="Phase"/>. Reuses the same "why" and "resume note"
/// ideas as <see cref="TaskItem"/> and the Phase 4 git-linking pattern.
/// </summary>
public class Subtask
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    /// <summary>
    /// The owning phase. Null for Kanban and other free-board items, whose column
    /// is decided by <see cref="Status"/> alone.
    /// </summary>
    public int? PhaseId { get; set; }

    public Phase? Phase { get; set; }

    public string Title { get; set; } = string.Empty;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    /// <summary>
    /// When the work is planned to start, for the Gantt timeline. Optional: when null,
    /// Waterfall projects fall back to the end of the previous phase (see GanttSchedule).
    /// </summary>
    public DateTime? StartDate { get; set; }

    public DateTime? DueDate { get; set; }

    public double? EstimatedHours { get; set; }

    public double? ActualHours { get; set; }

    public SubtaskStatus Status { get; set; } = SubtaskStatus.Todo;

    /// <summary>Who's doing this. Nullable — not every subtask needs an owner.</summary>
    public int? AssignedToId { get; set; }

    public TeamMember? AssignedTo { get; set; }

    /// <summary>Why the work stalled; shown on the Blocked badge. Null unless blocked.</summary>
    public string? BlockedReason { get; set; }

    /// <summary>Why this subtask matters (same idea as <see cref="TaskItem.WhyReason"/>).</summary>
    public string? WhyReason { get; set; }

    /// <summary>Where you left off (same idea as <see cref="TaskItem.ContextResumeNote"/>).</summary>
    public string? ContextResumeNote { get; set; }

    /// <summary>A branch name or commit tag that auto-completes the subtask, reusing the git watcher.</summary>
    public string? GitLinkPattern { get; set; }

    /// <summary>Which iteration this belongs to; only set for Iterative projects.</summary>
    public int? IterationNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsDone => Status == SubtaskStatus.Done;
}
