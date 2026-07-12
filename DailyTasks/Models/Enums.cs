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
/// the detail view, and the phase-gating rules.
/// </summary>
public enum Methodology
{
    Waterfall = 0,
    Agile = 1,
    Iterative = 2,
    Kanban = 3,
    Custom = 4,
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
