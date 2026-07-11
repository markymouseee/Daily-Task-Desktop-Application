using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Owns the create/open windows for projects so view models stay free of WPF types.
/// </summary>
public interface IProjectCoordinator
{
    /// <summary>
    /// Shows the create dialog. Persists whatever the user builds — a Simple task or a
    /// Project — and returns the new project, or null if they cancelled or made a simple task.
    /// </summary>
    Task<Project?> CreateAsync();

    /// <summary>Opens the detail window for a project and returns once it closes.</summary>
    Task OpenDetailAsync(int projectId);
}
