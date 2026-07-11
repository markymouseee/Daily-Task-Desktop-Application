using DailyTasks.Models;

namespace DailyTasks.Services;

public interface IProjectService
{
    /// <summary>
    /// Turns a freshly-built <see cref="TaskItem"/> into a project: persists the task
    /// as a Project type, creates the project row, and seeds its default phase set for
    /// the chosen methodology. Returns the loaded project graph.
    /// </summary>
    Task<Project> CreateProjectAsync(
        TaskItem task,
        Methodology methodology,
        IReadOnlyList<string>? customPhases = null,
        int? iterationCount = null);

    /// <summary>The full project graph (task, phases, subtasks) for a project id, or null.</summary>
    Task<Project?> GetAsync(int projectId);

    /// <summary>The full project graph headed by a given task, or null if it isn't a project.</summary>
    Task<Project?> GetByTaskIdAsync(int taskItemId);

    /// <summary>Every project with its graph loaded, for list/Today rendering.</summary>
    Task<IReadOnlyList<Project>> GetAllAsync();

    Task AddSubtaskAsync(Subtask subtask);

    Task UpdateSubtaskAsync(Subtask subtask);

    Task DeleteSubtaskAsync(Subtask subtask);

    /// <summary>Persists the locked/unlocked state of the given phases (Waterfall gating).</summary>
    Task UpdatePhaseLocksAsync(IEnumerable<Phase> phases);
}
