using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// The view-model side of opening task dialogs, so list view models never
/// construct windows themselves.
/// </summary>
public interface ITaskEditor
{
    /// <summary>Opens the task editor; persists and returns true when saved.</summary>
    Task<bool> EditAsync(TaskItem task);

    /// <summary>Asks for a one-line "where I left off" note after an abandoned focus session.</summary>
    Task PromptResumeNoteAsync(TaskItem task);
}
