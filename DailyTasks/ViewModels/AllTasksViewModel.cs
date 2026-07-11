using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

public partial class AllTasksViewModel(ITaskService tasks, FocusService focus, ITaskEditor editor)
    : TaskListViewModel(tasks, focus, editor)
{
    public override string EmptyMessage => "No tasks yet. Head to Today to add your first one.";

    // Project heads live on the Projects hub and open the project detail, not the
    // plain task editor, so they're kept out of this flat list.
    protected override bool Includes(TaskItem task) =>
        task.TaskType == TaskType.Simple && !task.IsCompleted;
}
