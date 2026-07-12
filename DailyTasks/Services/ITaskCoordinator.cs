using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>Opens the organize picker and the methodology detail window (keeps WPF out of VMs).</summary>
public interface ITaskCoordinator
{
    /// <summary>Shows the methodology picker for a task with children. Returns true if it changed.</summary>
    Task<bool> OrganizeAsync(TaskItem task);

    /// <summary>New-task dialog (title, category, priority, due). Persists it; returns true if created.</summary>
    Task<bool> CreateTaskAsync();

    /// <summary>Opens the phases / Kanban / Gantt / export detail window for an organized task.</summary>
    Task OpenDetailAsync(int taskId);

    /// <summary>
    /// New-project flow: name it, pick a methodology, then open its detail. Returns the new
    /// task's id, or null if cancelled.
    /// </summary>
    Task<int?> CreateProjectAsync();
}
