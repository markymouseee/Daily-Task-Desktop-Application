using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

public partial class CompletedViewModel(ITaskService tasks, FocusService focus, ITaskEditor editor)
    : TaskListViewModel(tasks, focus, editor)
{
    public override string EmptyMessage => "Nothing completed yet.";

    // Completed projects stay on the Projects hub rather than showing here as plain cards.
    protected override bool Includes(TaskItem task) =>
        task.TaskType == TaskType.Simple && task.IsCompleted;
}
