using DailyTasks.Models;

namespace DailyTasks.Services;

public interface ITaskService
{
    /// <summary>Raised after a task is persisted, so open views pick up tasks created elsewhere.</summary>
    event EventHandler<TaskItem>? TaskAdded;

    /// <summary>Top-level tasks, each with its full subtree and phases wired in memory.</summary>
    Task<IReadOnlyList<TaskItem>> GetRootsAsync();

    /// <summary>A single task with its subtree and phases wired, or null.</summary>
    Task<TaskItem?> GetAsync(int id);

    /// <summary>Adds a task. Set <see cref="TaskItem.ParentTaskId"/> for a child.</summary>
    Task AddAsync(TaskItem task);

    Task UpdateAsync(TaskItem task);

    /// <summary>
    /// Persists a completed task and — if it recurs — spawns the next occurrence
    /// (raising <see cref="TaskAdded"/>). Returns the spawned task, or null.
    /// </summary>
    Task<TaskItem?> CompleteAsync(TaskItem task);

    /// <summary>Deletes a task and its whole subtree.</summary>
    Task DeleteAsync(TaskItem task);

    /// <summary>
    /// Organizes a task as a methodology: sets it on the task and seeds the default phase
    /// set. Persists and returns the reloaded task graph.
    /// </summary>
    Task<TaskItem?> OrganizeAsync(TaskItem task, Methodology methodology, IReadOnlyList<string>? customPhases = null, int? iterationCount = null);

    /// <summary>Clears a task's methodology and removes its phases (children keep their place).</summary>
    Task ClearMethodologyAsync(TaskItem task);

    /// <summary>Persists the locked/unlocked state of the given phases (Waterfall gating).</summary>
    Task UpdatePhaseLocksAsync(IEnumerable<Phase> phases);
}

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
}
