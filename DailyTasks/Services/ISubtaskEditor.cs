using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Opens the subtask editor dialog (a child task's fuller editor: status, dates, hours,
/// assignee…). Mutates the passed task in place and reports whether the user saved.
/// </summary>
public interface ISubtaskEditor
{
    Task<bool> EditAsync(TaskItem subtask, bool developerFeatures);
}
