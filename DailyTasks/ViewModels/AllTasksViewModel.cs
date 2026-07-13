using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

public partial class AllTasksViewModel(ITaskService tasks, FocusService focus, ITaskEditor editor, ITaskCoordinator coordinator)
    : TaskListViewModel(tasks, focus, editor, coordinator)
{
    public override string EmptyMessage => "No tasks yet. Head to Today to add your first one.";

    // Organized project heads live on the Projects page, not among plain tasks.
    protected override bool Includes(TaskItem task) => !task.IsCompleted && task.Methodology is null;
}
