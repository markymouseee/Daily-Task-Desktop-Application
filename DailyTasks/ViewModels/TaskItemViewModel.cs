using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Models;

namespace DailyTasks.ViewModels;

/// <summary>
/// A single task card. Wraps <see cref="TaskItem"/> rather than duplicating its state.
/// </summary>
public partial class TaskItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isCompleted;

    public TaskItemViewModel(TaskItem model)
    {
        Model = model;

        // Assign the backing field directly: setting the property here would
        // raise CompletionChanged and write back a task we just loaded.
        _isCompleted = model.IsCompleted;
    }

    /// <summary>Raised after the user ticks or unticks the checkbox.</summary>
    public event EventHandler? CompletionChanged;

    public TaskItem Model { get; }

    public string Title => Model.Title;

    public string CategoryName => Model.Category.Name;

    public string CategoryColor => Model.Category.ColorHex;

    public TaskPriority Priority => Model.Priority;

    public DateTime? DueDate => Model.DueDate;

    public DateTime? CompletedAt => Model.CompletedAt;

    /// <summary>
    /// Sort key for due date. Undated tasks sort last rather than first,
    /// which is what a plain ascending sort over a nullable would do.
    /// </summary>
    public DateTime DueSortKey => Model.DueDate ?? DateTime.MaxValue;

    partial void OnIsCompletedChanged(bool value)
    {
        Model.IsCompleted = value;
        Model.CompletedAt = value ? DateTime.Now : null;
        CompletionChanged?.Invoke(this, EventArgs.Empty);
    }
}
