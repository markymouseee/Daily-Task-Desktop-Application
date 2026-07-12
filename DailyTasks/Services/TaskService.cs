using DailyTasks.Data;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Services;

/// <summary>
/// Each call uses a short-lived context; the UI holds detached task graphs. Loads
/// everything flat and links the tree in memory (see <see cref="TaskTree"/>).
/// </summary>
public sealed class TaskService(IDbContextFactory<AppDbContext> factory) : ITaskService
{
    public event EventHandler<TaskItem>? TaskAdded;

    public async Task<IReadOnlyList<TaskItem>> GetRootsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await LoadRootsAsync(db);
    }

    public async Task<TaskItem?> GetAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var roots = await LoadRootsAsync(db);
        return FindInForest(roots, id);
    }

    private static async Task<IReadOnlyList<TaskItem>> LoadRootsAsync(AppDbContext db)
    {
        var tasks = await db.Tasks
            .AsNoTracking()
            .Include(t => t.Category)
            .Include(t => t.AssignedTo)
            .ToListAsync();

        var phases = await db.Phases.AsNoTracking().ToListAsync();

        return TaskTree.Build(tasks, phases);
    }

    private static TaskItem? FindInForest(IEnumerable<TaskItem> roots, int id)
    {
        foreach (var root in roots)
        {
            if (root.Id == id)
            {
                return root;
            }

            var found = TaskTree.Descendants(root).FirstOrDefault(t => t.Id == id);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    public async Task AddAsync(TaskItem task)
    {
        await using var db = await factory.CreateDbContextAsync();
        AttachCategory(db, task);
        DetachNavigations(task);

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        TaskAdded?.Invoke(this, task);
    }

    public async Task UpdateAsync(TaskItem task)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.Tasks.FindAsync(task.Id);
        if (existing is null)
        {
            return;
        }

        // Copies scalars + FKs (CategoryId/ParentTaskId/PhaseId/AssignedToId), ignoring navs.
        db.Entry(existing).CurrentValues.SetValues(task);
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

    public async Task DeleteAsync(TaskItem task)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.Tasks.FindAsync(task.Id);
        if (existing is not null)
        {
            db.Tasks.Remove(existing); // cascades the subtree and this task's phases
            await db.SaveChangesAsync();
        }
    }

    public async Task<TaskItem?> OrganizeAsync(TaskItem task, Methodology methodology, IReadOnlyList<string>? customPhases = null, int? iterationCount = null)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.Tasks.Include(t => t.Phases).FirstOrDefaultAsync(t => t.Id == task.Id);
        if (existing is null)
        {
            return null;
        }

        existing.Methodology = methodology;
        existing.CustomPhases = methodology == Methodology.Custom ? (customPhases ?? []).ToList() : [];
        existing.IterationCount = methodology == Methodology.Iterative ? Math.Max(1, iterationCount ?? 1) : null;

        // Reseed phases from scratch.
        db.Phases.RemoveRange(existing.Phases);

        foreach (var (name, order) in PhaseNamesFor(methodology, customPhases).Select((n, i) => (n, i)))
        {
            existing.Phases.Add(new Phase
            {
                OwnerTaskId = existing.Id,
                Name = name,
                Order = order,
                IsLocked = methodology == Methodology.Waterfall && order > 0,
            });
        }

        await db.SaveChangesAsync();
        return await GetAsync(task.Id);
    }

    public async Task ClearMethodologyAsync(TaskItem task)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.Tasks.Include(t => t.Phases).FirstOrDefaultAsync(t => t.Id == task.Id);
        if (existing is null)
        {
            return;
        }

        existing.Methodology = null;
        existing.CustomPhases = [];
        existing.IterationCount = null;
        db.Phases.RemoveRange(existing.Phases); // children's PhaseId falls to null (SetNull)

        await db.SaveChangesAsync();
    }

    public async Task UpdatePhaseLocksAsync(IEnumerable<Phase> phases)
    {
        var list = phases.ToList();
        if (list.Count == 0)
        {
            return;
        }

        await using var db = await factory.CreateDbContextAsync();

        foreach (var phase in list)
        {
            var existing = await db.Phases.FindAsync(phase.Id);
            if (existing is not null && existing.IsLocked != phase.IsLocked)
            {
                existing.IsLocked = phase.IsLocked;
            }
        }

        await db.SaveChangesAsync();
    }

    private static IReadOnlyList<string> PhaseNamesFor(Methodology methodology, IReadOnlyList<string>? customPhases) =>
        methodology == Methodology.Custom
            ? (customPhases ?? []).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList()
            : TaskRules.DefaultPhaseNames(methodology);

    private static TaskItem BuildNextOccurrence(TaskItem task) => new()
    {
        Title = task.Title,
        Notes = task.Notes,
        CategoryId = task.CategoryId,
        Category = task.Category,
        Priority = task.Priority,
        EstimatedHours = task.EstimatedHours,
        WhyReason = task.WhyReason,
        GitLink = task.GitLink,
        Recurrence = task.Recurrence,
        DueDate = NextDueDate(task.DueDate ?? DateTime.Today, task.Recurrence),
    };

    private static DateTime NextDueDate(DateTime from, RecurrenceKind kind)
    {
        var date = from.Date;
        var today = DateTime.Today;

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

    private static void AttachCategory(AppDbContext db, TaskItem task)
    {
        if (task.Category is not null)
        {
            db.Attach(task.Category);
        }
    }

    /// <summary>Callers pass FK scalars; drop navs loaded elsewhere so EF doesn't re-insert them.</summary>
    private static void DetachNavigations(TaskItem task)
    {
        task.Parent = null;
        task.Phase = null;
        task.AssignedTo = null;
        task.Children.Clear();
        task.Phases.Clear();
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
