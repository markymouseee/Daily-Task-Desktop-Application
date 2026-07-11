using System.Windows;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.Views;

public sealed class SubtaskEditor : ISubtaskEditor
{
    public Task<bool> EditAsync(Subtask subtask, bool developerFeatures)
    {
        var window = new SubtaskEditWindow(subtask, developerFeatures)
        {
            Owner = Application.Current.MainWindow,
        };

        return Task.FromResult(window.ShowDialog() == true);
    }
}
