using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

public partial class AllTasksViewModel(ITaskService tasks, FocusService focus, ITaskEditor editor)
    : TaskListViewModel(tasks, focus, editor)
{
    public override string EmptyMessage => "No tasks yet. Head to Today to add your first one.";

    protected override bool Includes(TaskItem task) => !task.IsCompleted;
}
