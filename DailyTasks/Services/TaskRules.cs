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
    /// <summary>
    /// The out-of-the-box phase names for a methodology, in order. For the cyclical
    /// methodologies this is one cycle's worth (repeated per <see cref="TaskItem.IterationNumber"/>);
    /// for the board/flat methodologies it's empty. Sprint-based heads seed a different
    /// list — see <see cref="SeedPhaseNames"/>.
    /// </summary>
    public static IReadOnlyList<string> DefaultPhaseNames(Methodology methodology) => methodology switch
    {
        Methodology.Waterfall =>
            ["Requirements", "Design", "Implementation", "Verification", "Maintenance"],

        // V-Model: development phases first, then their paired testing phases, so the
        // ordered list reads down the left arm of the V and back up the right.
        Methodology.VModel =>
        [
            "Requirements", "System Design", "Architecture Design", "Module Design",
            "Unit Testing", "Integration Testing", "System Testing", "Acceptance Testing",
        ],

        Methodology.Spiral =>
            ["Objectives & Planning", "Risk Analysis & Prototyping", "Engineering", "Evaluation"],

        Methodology.IterativeIncremental => ["Plan", "Build", "Test", "Review"],

        Methodology.RAD => ["Prototype", "User Feedback", "Refine"],

        // DevOps pipeline stages, rendered as a looping pipeline rather than dated bars.
        Methodology.DevOps =>
            ["Plan", "Code", "Build", "Test", "Release", "Deploy", "Operate", "Monitor"],

        // Board / flat methodologies own no phases (columns come from WorkStatus).
        Methodology.Kanban or Methodology.Lean or Methodology.BigBang => [],

        // Agile-based heads seed Backlog + sprints instead; see SeedPhaseNames.
        Methodology.Agile or Methodology.Scrum or Methodology.XP => [],

        _ => [],
    };

    /// <summary>
    /// The phase rows to create when a task is first organized. Sprint-based methodologies
    /// get a Backlog pool followed by <paramref name="count"/> sprint/iteration phases;
    /// everything else uses <see cref="DefaultPhaseNames"/>.
    /// </summary>
    public static IReadOnlyList<string> SeedPhaseNames(Methodology methodology, int count)
    {
        if (!IsSprintBased(methodology))
        {
            return DefaultPhaseNames(methodology);
        }

        var label = methodology == Methodology.XP ? "Iteration" : "Sprint";
        var names = new List<string> { "Backlog" };
        for (var i = 1; i <= Math.Max(1, count); i++)
        {
            names.Add($"{label} {i}");
        }

        return names;
    }

    /// <summary>
    /// V-Model development phase → its paired testing phase, keyed by name. The dev phase
    /// stores the pairing (<see cref="Phase.PairedPhaseId"/>); the test phase does not.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> VModelPairs = new Dictionary<string, string>
    {
        ["Requirements"] = "Acceptance Testing",
        ["System Design"] = "System Testing",
        ["Architecture Design"] = "Integration Testing",
        ["Module Design"] = "Unit Testing",
    };

    /// <summary>How this methodology is visualized. Derived, never user-chosen.</summary>
    public static ChartType ChartTypeFor(Methodology methodology) => methodology switch
    {
        Methodology.Waterfall => ChartType.SequentialGantt,
        Methodology.VModel => ChartType.VShapedGantt,
        Methodology.Spiral or Methodology.IterativeIncremental or Methodology.RAD => ChartType.CyclicalGantt,
        Methodology.Agile or Methodology.Scrum or Methodology.XP => ChartType.AgileGantt,
        Methodology.Kanban or Methodology.Lean or Methodology.DevOps => ChartType.BoardOnly,
        Methodology.BigBang => ChartType.FlatListOnly,
        _ => ChartType.SequentialGantt,
    };

    /// <summary>Cyclical methodologies repeat their phase sequence once per iteration.</summary>
    public static bool UsesCycles(Methodology methodology) =>
        methodology is Methodology.Spiral or Methodology.IterativeIncremental or Methodology.RAD;

    /// <summary>Agile-based methodologies organize work into a Backlog plus sprint phases.</summary>
    public static bool IsSprintBased(Methodology methodology) =>
        methodology is Methodology.Agile or Methodology.Scrum or Methodology.XP;

    /// <summary>The board methodologies group children by <see cref="WorkStatus"/>, not phases.</summary>
    public static bool IsBoard(Methodology methodology) =>
        methodology is Methodology.Kanban or Methodology.Lean;

    /// <summary>Default sprint/iteration length in days for the agile methodologies.</summary>
    public const int DefaultSprintLengthDays = 14;

    /// <summary>Human-friendly methodology name for badges and the picker.</summary>
    public static string DisplayName(Methodology methodology) => methodology switch
    {
        Methodology.VModel => "V-Model",
        Methodology.IterativeIncremental => "Iterative & Incremental",
        Methodology.RAD => "RAD",
        Methodology.XP => "XP",
        Methodology.BigBang => "Big Bang",
        Methodology.DevOps => "DevOps",
        _ => methodology.ToString(),
    };

    /// <summary>One-line definition shown beside each methodology in the picker.</summary>
    public static string Description(Methodology methodology) => methodology switch
    {
        Methodology.Waterfall => "Requirements → Design → Implementation → Verification → Maintenance, each locked until the previous is 100% done.",
        Methodology.VModel => "Each development phase paired with its testing phase — Design ↔ System Testing, and so on, forming a V.",
        Methodology.Spiral => "Repeating risk-driven cycles: Objectives → Risk Analysis & Prototyping → Engineering → Evaluation.",
        Methodology.IterativeIncremental => "Repeating build cycles — Plan → Build → Test → Review — with no dedicated risk phase.",
        Methodology.RAD => "Short, fast cycles of Prototype → User Feedback → Refine to converge quickly.",
        Methodology.Agile => "A backlog feeding time-boxed iterations, reviewed at the end — no formal Scrum roles.",
        Methodology.Scrum => "Backlog → fixed-length sprints → review/retro, with optional Scrum roles on team members.",
        Methodology.XP => "Short iterations like Scrum, with per-task practice tags (Pair Programming, Test-Driven, Code Review).",
        Methodology.Kanban => "A flexible board — To Do → In Progress → Review → Done. No phases, no dates.",
        Methodology.Lean => "A Kanban board with WIP limits per column to keep flow smooth and expose bottlenecks.",
        Methodology.DevOps => "A continuous pipeline: Plan → Code → Build → Test → Release → Deploy → Operate → Monitor, looping back.",
        Methodology.BigBang => "No phases or structure — just a flat task list. The simplest possible view.",
        _ => string.Empty,
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
