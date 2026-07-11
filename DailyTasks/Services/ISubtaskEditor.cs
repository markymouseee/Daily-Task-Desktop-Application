using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Opens the subtask editor dialog. Keeps WPF out of the detail view model, mirroring
/// <see cref="ITaskEditor"/>. Mutates the passed subtask in place and reports whether
/// the user saved.
/// </summary>
public interface ISubtaskEditor
{
    Task<bool> EditAsync(Subtask subtask, bool developerFeatures);
}
