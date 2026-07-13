using System.Windows;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.Views;

public sealed class SubtaskEditor(ITeamService team) : ISubtaskEditor
{
    public async Task<bool> EditAsync(TaskItem subtask, int projectId, bool developerFeatures, bool showXpPractices = false)
    {
        var members = await team.GetForProjectAsync(projectId);

        var window = new SubtaskEditWindow(subtask, developerFeatures, members, showXpPractices)
        {
            Owner = Application.Current.MainWindow,
        };

        return window.ShowDialog() == true;
    }
}
