namespace DailyTasks.Models;

/// <summary>
/// The structured half of a Project-type <see cref="TaskItem"/>. Holds the chosen
/// methodology plus the phases and subtasks that make up its SDLC plan.
/// </summary>
public class Project
{
    public int Id { get; set; }

    /// <summary>The task this project hangs off; one project per task.</summary>
    public int TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;

    public Methodology Methodology { get; set; } = Methodology.Waterfall;

    /// <summary>
    /// The phase names entered by hand; only meaningful when
    /// <see cref="Methodology"/> is <see cref="Methodology.Custom"/>.
    /// Stored as a JSON array via a value converter.
    /// </summary>
    public List<string> CustomPhases { get; set; } = [];

    /// <summary>
    /// How many iterations to plan; only meaningful when
    /// <see cref="Methodology"/> is <see cref="Methodology.Iterative"/>.
    /// </summary>
    public int? IterationCount { get; set; }

    public ICollection<Phase> Phases { get; } = [];

    public ICollection<Subtask> Subtasks { get; } = [];
}
