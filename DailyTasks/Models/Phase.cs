namespace DailyTasks.Models;

/// <summary>
/// A named stage within a methodology-organized task (e.g. "Design"). For Waterfall these
/// are sequential and gate one another; for Agile/Iterative they group child tasks without
/// hard gating. Kanban uses <see cref="TaskItem.Status"/> for its columns and needs no phases.
/// </summary>
public class Phase
{
    public int Id { get; set; }

    /// <summary>The task these phases belong to.</summary>
    public int OwnerTaskId { get; set; }

    public TaskItem OwnerTask { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Left-to-right position of the phase; also the gating order for Waterfall.</summary>
    public int Order { get; set; }

    /// <summary>
    /// True while a Waterfall phase is still gated behind an unfinished earlier phase.
    /// Always false for non-gating methodologies.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>Child tasks placed in this phase.</summary>
    public ICollection<TaskItem> Tasks { get; } = [];
}
