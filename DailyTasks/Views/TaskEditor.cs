using System.Windows;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.Views;

public sealed class TaskEditor(ITaskService tasks) : ITaskEditor
{
    public async Task<bool> EditAsync(TaskItem task)
    {
        var window = new EditTaskWindow(task) { Owner = Application.Current.MainWindow };

        if (window.ShowDialog() != true)
        {
            return false;
        }

        await tasks.UpdateAsync(task);
        return true;
    }

    public async Task PromptResumeNoteAsync(TaskItem task)
    {
        var window = new ResumeNoteWindow(task.ContextResumeNote) { Owner = Application.Current.MainWindow };

        if (window.ShowDialog() != true)
        {
            return;
        }

        task.ContextResumeNote = string.IsNullOrWhiteSpace(window.Note) ? null : window.Note.Trim();
        await tasks.UpdateAsync(task);
    }
}
