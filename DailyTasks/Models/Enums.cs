namespace DailyTasks.Models;

/// <summary>
/// Values are ordered so that a descending sort puts High first.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
}

/// <summary>What pulled the user away during a focus session.</summary>
public enum InterruptionReason
{
    Other = 0,
    Meeting = 1,
    Message = 2,
    Person = 3,
}

/// <summary>How a task re-creates itself after completion.</summary>
public enum RecurrenceKind
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
}

/// <summary>
/// The SDLC shape of a methodology-organized task. Drives the default phase set,
/// the detail view, the <see cref="ChartType"/> and the phase-gating rules. Stored as
/// text (see <c>AppDbContext</c>), so members can be reordered freely; renaming or
/// removing a member instead invalidates rows that stored the old name.
/// </summary>
public enum Methodology
{
    // Sequential
    Waterfall,
    VModel,

    // Iterative / cyclical
    Spiral,
    IterativeIncremental,
    RAD,

    // Agile-based
    Agile,
    Scrum,
    XP,

    // Continuous flow
    Kanban,
    Lean,
    DevOps,

    // Minimal
    BigBang,
}

/// <summary>
/// How a methodology-organized task is visualized. Derived automatically from
/// <see cref="Methodology"/> (never chosen independently) so the picture is always
/// structurally correct for the process. See <c>TaskRules.ChartTypeFor</c>.
/// </summary>
public enum ChartType
{
    /// <summary>Phase bars in a fixed order with dependency connectors (Waterfall).</summary>
    SequentialGantt,

    /// <summary>Two linked rows — dev phases up top, paired test phases below (V-Model).</summary>
    VShapedGantt,

    /// <summary>Repeating, bordered cycle blocks (Spiral, Iterative &amp; Incremental, RAD).</summary>
    CyclicalGantt,

    /// <summary>Sprint-grouped row Gantt with a calendar timeline (Agile, Scrum, XP).</summary>
    AgileGantt,

    /// <summary>A board or pipeline, no dated timeline (Kanban, Lean, DevOps).</summary>
    BoardOnly,

    /// <summary>A flat, unordered task list — no phases at all (Big Bang).</summary>
    FlatListOnly,
}

/// <summary>Optional Scrum role tag carried by a <see cref="TeamMember"/>.</summary>
public enum ScrumRole
{
    None = 0,
    ScrumMaster = 1,
    ProductOwner = 2,
    DevTeam = 3,
}

/// <summary>
/// XP engineering practices a subtask can be tagged with, shown as small icon badges on
/// its card. Flags so a task can carry several at once.
/// </summary>
[Flags]
public enum XpPractice
{
    None = 0,
    PairProgramming = 1,
    TestDriven = 2,
    CodeReview = 4,
}

/// <summary>
/// A task's workflow state. Ordered so a Kanban board reads left-to-right and a
/// descending sort surfaces the least-finished work first. Applies to every task;
/// for plain tasks only Todo/Done are used (via the completion checkbox).
/// </summary>
public enum WorkStatus
{
    Todo = 0,
    InProgress = 1,
    Review = 2,
    Done = 3,
    Blocked = 4,
}
