namespace DailyTasks.Services;

/// <summary>Opens the team-management window; keeps WPF out of the view models.</summary>
public interface ITeamCoordinator
{
    void OpenManager();
}
