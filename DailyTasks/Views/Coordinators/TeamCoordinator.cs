using System.Windows;
using DailyTasks.Services;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

public sealed class TeamCoordinator(ITeamService team) : ITeamCoordinator
{
    public void OpenManager()
    {
        var window = new TeamWindow(new TeamViewModel(team))
        {
            Owner = Application.Current.MainWindow,
        };

        window.ShowDialog();
    }
}
