namespace DailyTasks.Models;

/// <summary>
/// A named stage within a project (e.g. "Design"). For Waterfall these are
/// sequential and gate one another; for Agile/Iterative they group subtasks
/// without hard gating. Kanban uses <see cref="Subtask.Status"/> for its columns
/// and needs no phases.
/// </summary>
public class Phase
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Left-to-right position of the phase; also the gating order for Waterfall.</summary>
    public int Order { get; set; }

    /// <summary>
    /// True while a Waterfall phase is still gated behind an unfinished earlier
    /// phase. Recomputed as subtasks complete; always false for non-gating methodologies.
    /// </summary>
    public bool IsLocked { get; set; }

    public ICollection<Subtask> Subtasks { get; } = [];
}
