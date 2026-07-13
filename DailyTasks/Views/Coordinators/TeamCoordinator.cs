using System.Windows;
using DailyTasks.Services;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

public sealed class TeamCoordinator(ITeamService team) : ITeamCoordinator
{
    public void OpenManager(int projectId, string projectTitle)
    {
        var window = new TeamWindow(new TeamViewModel(team, projectId, projectTitle))
        {
            Owner = Application.Current.MainWindow,
        };

        window.ShowDialog();
    }
}
