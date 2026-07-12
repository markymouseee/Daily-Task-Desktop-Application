using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Wires a flat set of tasks (+ phases) into an in-memory tree and offers subtree helpers.
/// The app loads everything flat and links it here rather than doing arbitrary-depth
/// Includes in EF.
/// </summary>
public static class TaskTree
{
    /// <summary>
    /// Links Parent/Children, Phase and each task's owned Phases, then returns the roots
    /// (tasks with no parent). Mutates the passed entities' navigation properties.
    /// </summary>
    public static IReadOnlyList<TaskItem> Build(IReadOnlyList<TaskItem> tasks, IReadOnlyList<Phase> phases)
    {
        var byId = tasks.ToDictionary(t => t.Id);
        var phaseById = phases.ToDictionary(p => p.Id);

        // Clear any pre-existing links (entities may be reused across loads).
        foreach (var task in tasks)
        {
            task.Children.Clear();
            task.Phases.Clear();
            task.Parent = null;
            task.Phase = null;
        }

        foreach (var phase in phases)
        {
            phase.Tasks.Clear();
            if (byId.TryGetValue(phase.OwnerTaskId, out var owner))
            {
                owner.Phases.Add(phase);
            }
        }

        foreach (var task in tasks)
        {
            if (task.ParentTaskId is { } pid && byId.TryGetValue(pid, out var parent))
            {
                task.Parent = parent;
                parent.Children.Add(task);
            }

            if (task.PhaseId is { } phId && phaseById.TryGetValue(phId, out var phase))
            {
                task.Phase = phase;
                phase.Tasks.Add(task);
            }
        }

        return tasks.Where(t => t.ParentTaskId is null).ToList();
    }

    /// <summary>All descendants of a task (children, grandchildren, …), excluding itself.</summary>
    public static IEnumerable<TaskItem> Descendants(TaskItem task)
    {
        foreach (var child in task.Children)
        {
            yield return child;
            foreach (var deeper in Descendants(child))
            {
                yield return deeper;
            }
        }
    }
}
