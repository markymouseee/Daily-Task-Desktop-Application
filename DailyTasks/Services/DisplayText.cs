using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Small shared formatters for values shown across the UI, so the same
/// <see cref="WorkStatus"/> wording and name handling live in exactly one place.
/// </summary>
public static class DisplayText
{
    /// <summary>Human-friendly workflow status ("In Progress", "To Do", …).</summary>
    public static string Label(this WorkStatus status) => status switch
    {
        WorkStatus.Todo => "To Do",
        WorkStatus.InProgress => "In Progress",
        WorkStatus.Review => "Review",
        WorkStatus.Done => "Done",
        WorkStatus.Blocked => "Blocked",
        _ => status.ToString(),
    };

    /// <summary>The first word of a person's name, for compact avatars/labels.</summary>
    public static string FirstName(string? fullName) =>
        fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
}
