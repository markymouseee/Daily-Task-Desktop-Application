namespace DailyTasks.Models;

/// <summary>
/// One press of "I got interrupted" during a focus session.
/// </summary>
public class InterruptionEvent
{
    public int Id { get; set; }

    /// <summary>Nullable and set-null on delete: interruption history outlives its task.</summary>
    public int? TaskItemId { get; set; }

    public TaskItem? TaskItem { get; set; }

    public DateTime OccurredAt { get; set; }

    public InterruptionReason Reason { get; set; } = InterruptionReason.Other;

    /// <summary>Pause-to-resume duration, measured rather than guessed.</summary>
    public int MinutesLost { get; set; }
}
