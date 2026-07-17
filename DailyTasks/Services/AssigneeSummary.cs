using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>One assignee rendered as an avatar chip.</summary>
public sealed record AssigneeChip(string Name, string FirstName, string ColorHex);

/// <summary>How an activity's assignees should be shown: "Team", a set of chips, or nothing.</summary>
public sealed record AssigneeDisplay(bool IsTeam, IReadOnlyList<AssigneeChip> Chips)
{
    public static readonly AssigneeDisplay None = new(false, []);

    public bool HasAny => IsTeam || Chips.Count > 0;
}

/// <summary>Shared rule for displaying multi-assignees, so every view agrees.</summary>
public static class AssigneeSummary
{
    /// <summary>
    /// Collapses to "Team" when an activity is assigned to the whole project team (2+ people);
    /// otherwise returns one chip per assignee, name-sorted.
    /// </summary>
    public static AssigneeDisplay Of(ICollection<TeamMember> assignees, int projectTeamCount)
    {
        if (assignees.Count == 0)
        {
            return AssigneeDisplay.None;
        }

        if (projectTeamCount > 1 && assignees.Count >= projectTeamCount)
        {
            return new AssigneeDisplay(IsTeam: true, []);
        }

        var chips = assignees
            .OrderBy(m => m.Name)
            .Select(m => new AssigneeChip(m.Name, DisplayText.FirstName(m.Name), m.InitialsColorHex))
            .ToList();

        return new AssigneeDisplay(IsTeam: false, chips);
    }
}
