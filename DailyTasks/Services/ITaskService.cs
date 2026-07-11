using DailyTasks.Models;

namespace DailyTasks.Services;

public interface ITaskService
{
    /// <summary>
    /// Raised after a task is persisted, so an open Today view picks up tasks
    /// created elsewhere (the global quick-capture bar).
    /// </summary>
    event EventHandler<TaskItem>? TaskAdded;

    Task<IReadOnlyList<TaskItem>> GetAllAsync();

    Task AddAsync(TaskItem task);

    Task UpdateAsync(TaskItem task);

    /// <summary>
    /// Persists a task the caller has just marked complete, and — if it recurs —
    /// spawns the next occurrence (raising <see cref="TaskAdded"/> for it).
    /// Returns the spawned task, or null when the task does not recur.
    /// </summary>
    Task<TaskItem?> CompleteAsync(TaskItem task);

    Task DeleteAsync(TaskItem task);
}

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
}
