using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>A phase's (or project's, or iteration's) completion at a glance.</summary>
public readonly record struct Progress(int Total, int Done, int Blocked)
{
    /// <summary>0–1. An empty group reads as 0% rather than a divide-by-zero.</summary>
    public double Fraction => Total == 0 ? 0 : (double)Done / Total;

    public int Percent => (int)Math.Round(Fraction * 100);

    /// <summary>A group is "complete" only once it has work and all of it is Done.</summary>
    public bool IsComplete => Total > 0 && Done == Total;

    public bool HasBlocked => Blocked > 0;

    public static Progress Of(IEnumerable<Subtask> subtasks)
    {
        int total = 0, done = 0, blocked = 0;

        foreach (var s in subtasks)
        {
            total++;
            if (s.Status == SubtaskStatus.Done)
            {
                done++;
            }
            else if (s.Status == SubtaskStatus.Blocked)
            {
                blocked++;
            }
        }

        return new Progress(total, done, blocked);
    }
}

/// <summary>
/// Pure phase-gating and progress logic, shared by the detail view models and the
/// Excel export so both agree on what "complete" and "locked" mean.
/// </summary>
public static class ProjectRules
{
    /// <summary>The out-of-the-box phase names for a methodology, in order.</summary>
    public static IReadOnlyList<string> DefaultPhaseNames(Methodology methodology) => methodology switch
    {
        // Sequential, each gated behind the previous reaching 100%.
        Methodology.Waterfall =>
            ["Requirements", "Design", "Implementation", "Testing", "Deployment", "Maintenance"],

        Methodology.Agile => ["Backlog", "Sprint", "Review", "Done"],

        // One shared set; each iteration reuses it, distinguished by IterationNumber.
        Methodology.Iterative => ["Plan", "Build", "Test", "Review"],

        // Kanban columns come straight from SubtaskStatus, so it owns no phases.
        Methodology.Kanban => [],

        // Custom phases are supplied by the user at creation time.
        Methodology.Custom => [],

        _ => [],
    };

    /// <summary>Only Waterfall gates phases sequentially.</summary>
    public static bool GatesPhases(Methodology methodology) => methodology == Methodology.Waterfall;

    /// <summary>
    /// Recomputes each phase's <see cref="Phase.IsLocked"/> in place: a Waterfall phase
    /// is locked until every earlier phase is complete. Returns the phases whose lock
    /// state actually changed, so callers persist only those.
    /// </summary>
    public static IReadOnlyList<Phase> RecomputeLocks(Project project)
    {
        var changed = new List<Phase>();
        var ordered = project.Phases.OrderBy(p => p.Order).ToList();

        if (!GatesPhases(project.Methodology))
        {
            foreach (var phase in ordered)
            {
                SetLock(phase, false, changed);
            }

            return changed;
        }

        // Group off the project's own subtask list rather than each phase's
        // back-navigation, which the loaded graph doesn't populate.
        var byPhase = project.Subtasks.ToLookup(s => s.PhaseId);
        var priorComplete = true;

        foreach (var phase in ordered)
        {
            SetLock(phase, !priorComplete, changed);

            // The next phase unlocks only once this one is finished, so a gap
            // (an unfinished phase) keeps everything after it locked.
            priorComplete = priorComplete && Progress.Of(byPhase[phase.Id]).IsComplete;
        }

        return changed;
    }

    private static bool SetLock(Phase phase, bool locked, List<Phase> changed)
    {
        if (phase.IsLocked == locked)
        {
            return false;
        }

        phase.IsLocked = locked;
        changed.Add(phase);
        return true;
    }
}
