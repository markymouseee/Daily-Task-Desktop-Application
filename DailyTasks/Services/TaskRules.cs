using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>A group of tasks' completion at a glance (phase, subtree, or iteration).</summary>
public readonly record struct Progress(int Total, int Done, int Blocked)
{
    /// <summary>0–1. An empty group reads as 0% rather than a divide-by-zero.</summary>
    public double Fraction => Total == 0 ? 0 : (double)Done / Total;

    public int Percent => (int)Math.Round(Fraction * 100);

    /// <summary>A group is "complete" only once it has work and all of it is Done.</summary>
    public bool IsComplete => Total > 0 && Done == Total;

    public bool HasBlocked => Blocked > 0;

    public static Progress Of(IEnumerable<TaskItem> tasks)
    {
        int total = 0, done = 0, blocked = 0;

        foreach (var t in tasks)
        {
            total++;
            if (t.Status == WorkStatus.Done)
            {
                done++;
            }
            else if (t.Status == WorkStatus.Blocked)
            {
                blocked++;
            }
        }

        return new Progress(total, done, blocked);
    }
}

/// <summary>
/// Pure phase-gating and progress logic for a methodology-organized task, shared by the
/// detail view models, the Gantt and the Excel export so they all agree on what
/// "complete" and "locked" mean. A task's phase members are its direct children.
/// </summary>
public static class TaskRules
{
    /// <summary>The out-of-the-box phase names for a methodology, in order.</summary>
    public static IReadOnlyList<string> DefaultPhaseNames(Methodology methodology) => methodology switch
    {
        Methodology.Waterfall =>
            ["Requirements", "Design", "Implementation", "Testing", "Deployment", "Maintenance"],

        Methodology.Agile => ["Backlog", "Sprint", "Review", "Done"],

        Methodology.Iterative => ["Plan", "Build", "Test", "Review"],

        // Kanban columns come straight from WorkStatus, so it owns no phases.
        Methodology.Kanban => [],

        // Custom phases are supplied by the user.
        Methodology.Custom => [],

        _ => [],
    };

    /// <summary>Only Waterfall gates phases sequentially.</summary>
    public static bool GatesPhases(Methodology methodology) => methodology == Methodology.Waterfall;

    /// <summary>
    /// Recomputes each phase's <see cref="Phase.IsLocked"/> in place for a methodology head:
    /// a Waterfall phase is locked until every earlier phase is complete. Returns the phases
    /// whose lock state changed, so callers persist only those.
    /// </summary>
    public static IReadOnlyList<Phase> RecomputeLocks(TaskItem head)
    {
        var changed = new List<Phase>();
        var ordered = head.Phases.OrderBy(p => p.Order).ToList();

        if (head.Methodology is not { } methodology || !GatesPhases(methodology))
        {
            foreach (var phase in ordered)
            {
                SetLock(phase, false, changed);
            }

            return changed;
        }

        var byPhase = head.Children.ToLookup(c => c.PhaseId);
        var priorComplete = true;

        foreach (var phase in ordered)
        {
            SetLock(phase, !priorComplete, changed);
            priorComplete = priorComplete && Progress.Of(byPhase[phase.Id]).IsComplete;
        }

        return changed;
    }

    private static void SetLock(Phase phase, bool locked, List<Phase> changed)
    {
        if (phase.IsLocked != locked)
        {
            phase.IsLocked = locked;
            changed.Add(phase);
        }
    }
}
