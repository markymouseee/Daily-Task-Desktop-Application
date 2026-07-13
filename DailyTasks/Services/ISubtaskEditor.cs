using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Opens the subtask editor dialog (a child task's fuller editor: status, dates, hours,
/// assignee…). Mutates the passed task in place and reports whether the user saved.
/// </summary>
public interface ISubtaskEditor
{
    /// <summary>
    /// Edits a subtask. The assignee picker is scoped to <paramref name="projectId"/>'s team.
    /// <paramref name="showXpPractices"/> reveals the XP practice tag checkboxes (only
    /// meaningful under an XP-organized head).
    /// </summary>
    Task<bool> EditAsync(TaskItem subtask, int projectId, bool developerFeatures, bool showXpPractices = false);
}
