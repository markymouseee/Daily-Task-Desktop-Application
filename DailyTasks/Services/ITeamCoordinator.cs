namespace DailyTasks.Services;

/// <summary>Opens the team-management window; keeps WPF out of the view models.</summary>
public interface ITeamCoordinator
{
    /// <summary>Manage the team for one project. Members are scoped to that project.</summary>
    void OpenManager(int projectId, string projectTitle);
}
