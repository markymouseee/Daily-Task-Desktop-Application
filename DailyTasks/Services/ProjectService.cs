using DailyTasks.Data;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Services;

/// <summary>
/// Short-lived context per call, mirroring <see cref="TaskService"/>; the UI holds
/// detached project graphs and calls back in to persist individual changes.
/// </summary>
public sealed class ProjectService(IDbContextFactory<AppDbContext> factory) : IProjectService
{
    public async Task<Project> CreateProjectAsync(
        TaskItem task,
        Methodology methodology,
        IReadOnlyList<string>? customPhases = null,
        int? iterationCount = null)
    {
        await using var db = await factory.CreateDbContextAsync();

        if (task.Category is not null)
        {
            db.Attach(task.Category);
        }

        task.TaskType = TaskType.Project;

        var project = new Project
        {
            TaskItem = task,
            Methodology = methodology,
            CustomPhases = methodology == Methodology.Custom
                ? (customPhases ?? []).ToList()
                : [],
            IterationCount = methodology == Methodology.Iterative ? Math.Max(1, iterationCount ?? 1) : null,
        };

        foreach (var (name, order) in PhaseNamesFor(methodology, customPhases).Select((n, i) => (n, i)))
        {
            project.Phases.Add(new Phase
            {
                Name = name,
                Order = order,
                // Waterfall locks everything past the first phase until it's finished.
                IsLocked = methodology == Methodology.Waterfall && order > 0,
            });
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return await LoadGraphAsync(db, p => p.Id == project.Id)
            ?? throw new InvalidOperationException("Project vanished immediately after creation.");
    }

    /// <summary>
    /// Custom takes the user's list; everything else takes the methodology default.
    /// Kanban intentionally yields nothing — its columns are the subtask statuses.
    /// </summary>
    private static IReadOnlyList<string> PhaseNamesFor(Methodology methodology, IReadOnlyList<string>? customPhases) =>
        methodology == Methodology.Custom
            ? (customPhases ?? []).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()).ToList()
            : ProjectRules.DefaultPhaseNames(methodology);

    public async Task<Project?> GetAsync(int projectId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await LoadGraphAsync(db, p => p.Id == projectId);
    }

    public async Task<Project?> GetByTaskIdAsync(int taskItemId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await LoadGraphAsync(db, p => p.TaskItemId == taskItemId);
    }

    public async Task<IReadOnlyList<Project>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();

        return await db.Projects
            .AsNoTracking()
            .Include(p => p.TaskItem).ThenInclude(t => t.Category)
            .Include(p => p.Phases)
            .Include(p => p.Subtasks).ThenInclude(s => s.AssignedTo)
            .ToListAsync();
    }

    private static async Task<Project?> LoadGraphAsync(
        AppDbContext db,
        System.Linq.Expressions.Expression<Func<Project, bool>> predicate) =>
        await db.Projects
            .AsNoTracking()
            .Include(p => p.TaskItem).ThenInclude(t => t.Category)
            .Include(p => p.Phases)
            .Include(p => p.Subtasks).ThenInclude(s => s.AssignedTo)
            .FirstOrDefaultAsync(predicate);

    public async Task AddSubtaskAsync(Subtask subtask)
    {
        await using var db = await factory.CreateDbContextAsync();

        // Callers pass FK scalars (ProjectId/PhaseId/AssignedToId); drop any navigation
        // loaded by another context so EF doesn't try to re-insert those rows.
        subtask.Project = null!;
        subtask.Phase = null;
        subtask.AssignedTo = null;

        db.Subtasks.Add(subtask);
        await db.SaveChangesAsync();
    }

    public async Task UpdateSubtaskAsync(Subtask subtask)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.Subtasks.FindAsync(subtask.Id);

        if (existing is null)
        {
            return;
        }

        // Copies scalars and FKs (ProjectId/PhaseId), leaving navigation untouched.
        db.Entry(existing).CurrentValues.SetValues(subtask);
        await db.SaveChangesAsync();
    }

    public async Task DeleteSubtaskAsync(Subtask subtask)
    {
        await using var db = await factory.CreateDbContextAsync();

        var existing = await db.Subtasks.FindAsync(subtask.Id);

        if (existing is not null)
        {
            db.Subtasks.Remove(existing);
            await db.SaveChangesAsync();
        }
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
}
