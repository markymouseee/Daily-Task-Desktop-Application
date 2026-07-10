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

    Task DeleteAsync(TaskItem task);
}

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
}
