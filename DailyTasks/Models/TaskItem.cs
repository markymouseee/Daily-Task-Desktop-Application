namespace DailyTasks.Models;

public class TaskItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public int? EstimatedMinutes { get; set; }

    public int? ActualMinutes { get; set; }

    public bool IsCompleted { get; set; }

    /// <summary>Incremented each time the task is rescheduled (Phase 3 stale-task nudge).</summary>
    public int PostponedCount { get; set; }

    /// <summary>Why this task matters, e.g. "so I don't get charged a late fee".</summary>
    public string? WhyReason { get; set; }

    /// <summary>Where you left off, shown when the task is reopened.</summary>
    public string? ContextResumeNote { get; set; }

    /// <summary>
    /// The day this task was pinned as one of the "Big 3". Comparing it against
    /// today is what makes the pin expire on its own each morning.
    /// </summary>
    public DateTime? BigThreeDate { get; set; }

    /// <summary>When the stale-task nudge last asked about this task, so it asks at most once a day.</summary>
    public DateTime? LastNudgedAt { get; set; }

    /// <summary>
    /// Developer feature: a branch name or commit-message tag (e.g. "closes #42").
    /// The git watcher auto-completes the task when a commit message contains it.
    /// </summary>
    public string? GitLink { get; set; }

    /// <summary>
    /// When set, completing the task spawns a fresh copy with its due date advanced
    /// by one interval. The spawned copy carries the same recurrence, so it repeats.
    /// </summary>
    public RecurrenceKind Recurrence { get; set; } = RecurrenceKind.None;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }
}
