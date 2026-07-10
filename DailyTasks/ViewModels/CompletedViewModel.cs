using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

public partial class CompletedViewModel(ITaskService tasks) : TaskListViewModel(tasks)
{
    public override string EmptyMessage => "Nothing completed yet.";

    protected override bool Includes(TaskItem task) => task.IsCompleted;
}
