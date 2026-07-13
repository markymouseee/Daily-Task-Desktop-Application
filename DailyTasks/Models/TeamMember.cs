namespace DailyTasks.Models;

/// <summary>
/// Someone work can be assigned to. Free-text <see cref="Role"/> keeps it flexible for
/// capstone and small dev teams; the avatar is a coloured initials badge — no photos, so
/// everything stays local and offline.
/// </summary>
public class TeamMember
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Free text, e.g. "Frontend Dev", "QA". Not an enum on purpose.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Optional Scrum role, surfaced as metadata under Scrum-organized tasks. Defaults to
    /// <see cref="ScrumRole.None"/> so it stays invisible for non-Scrum teams.
    /// </summary>
    public ScrumRole ScrumRole { get; set; } = ScrumRole.None;

    /// <summary>Display label for <see cref="ScrumRole"/> ("" for None). Not mapped (get-only).</summary>
    public string ScrumRoleText => ScrumRole switch
    {
        ScrumRole.ScrumMaster => "Scrum Master",
        ScrumRole.ProductOwner => "Product Owner",
        ScrumRole.DevTeam => "Dev Team",
        _ => string.Empty,
    };

    /// <summary>Avatar badge background, as "#RRGGBB".</summary>
    public string InitialsColorHex { get; set; } = "#3B82F6";

    /// <summary>Tasks currently assigned to this member.</summary>
    public ICollection<TaskItem> Tasks { get; } = [];
}
