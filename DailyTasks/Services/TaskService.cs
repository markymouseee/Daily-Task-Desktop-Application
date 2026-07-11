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

    public async Task<TaskItem?> CompleteAsync(TaskItem task)
    {
        await UpdateAsync(task);

        if (task.Recurrence == RecurrenceKind.None)
        {
            return null;
        }

        var next = BuildNextOccurrence(task);
        await AddAsync(next);
        return next;
    }

    /// <summary>
    /// A fresh copy of a recurring task with its due date rolled to the next
    /// future interval. Per-instance history (notes, actuals, pins) is not carried over.
    /// </summary>
    private static TaskItem BuildNextOccurrence(TaskItem task) => new()
    {
        Title = task.Title,
        Notes = task.Notes,
        CategoryId = task.CategoryId,
        Category = task.Category,
        Priority = task.Priority,
        EstimatedMinutes = task.EstimatedMinutes,
        WhyReason = task.WhyReason,
        GitLink = task.GitLink,
        Recurrence = task.Recurrence,
        DueDate = NextDueDate(task.DueDate ?? DateTime.Today, task.Recurrence),
    };

    private static DateTime NextDueDate(DateTime from, RecurrenceKind kind)
    {
        var date = from.Date;
        var today = DateTime.Today;

        // Roll forward until the next occurrence is in the future, so completing a
        // long-overdue recurring task creates one upcoming copy, not a backlog.
        do
        {
            date = kind switch
            {
                RecurrenceKind.Daily => date.AddDays(1),
                RecurrenceKind.Weekly => date.AddDays(7),
                RecurrenceKind.Monthly => date.AddMonths(1),
                _ => date.AddDays(1),
            };
        }
        while (date <= today);

        return date;
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
