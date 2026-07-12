using System.Windows;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.Views;

public sealed class SubtaskEditor(ITeamService team) : ISubtaskEditor
{
    public async Task<bool> EditAsync(TaskItem subtask, bool developerFeatures)
    {
        var members = await team.GetAllAsync();

        var window = new SubtaskEditWindow(subtask, developerFeatures, members)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true;
    }
}
