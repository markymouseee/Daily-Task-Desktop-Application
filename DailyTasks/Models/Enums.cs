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
