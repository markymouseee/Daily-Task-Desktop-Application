using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Exports a project's SDLC to Excel. Split so the file picker runs on the UI thread
/// while the (potentially slow) workbook write happens off it.
/// </summary>
public interface IProjectExporter
{
    /// <summary>Shows a Save dialog and returns the chosen path, or null if cancelled.</summary>
    string? PromptForPath(Project project);

    /// <summary>Writes the workbook to the path on a background thread.</summary>
    Task WriteAsync(Project project, string path);
}
