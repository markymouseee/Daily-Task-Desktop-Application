using DailyTasks.Data;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Services;

/// <summary>
/// Each call uses a short-lived context; the UI holds detached entities.
/// </summary>
public sealed class TaskService(IDbContextFactory<AppDbContext> factory) : ITaskService
{
    public event EventHandler<TaskItem>? TaskAdded;

    public async Task<IReadOnlyList<TaskItem>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Tasks.AsNoTracking().Include(t => t.Category).ToListAsync();
    }

    public async Task AddAsync(TaskItem task)
    {
        await using var db = await factory.CreateDbContextAsync();
        AttachCategory(db, task);
        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        TaskAdded?.Invoke(this, task);
    }

    public async Task UpdateAsync(TaskItem task)
    {
        await using var db = await factory.CreateDbContextAsync();
        AttachCategory(db, task);
        db.Tasks.Update(task);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(TaskItem task)
    {
        await using var db = await factory.CreateDbContextAsync();
        AttachCategory(db, task);
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// The task carries a Category loaded by another context. Tracking it as
    /// Unchanged first stops EF from treating it as a new row to insert.
    /// </summary>
    private static void AttachCategory(AppDbContext db, TaskItem task)
    {
        if (task.Category is not null)
        {
            db.Attach(task.Category);
        }
    }
}

public sealed class CategoryService(IDbContextFactory<AppDbContext> factory) : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
    }
}
