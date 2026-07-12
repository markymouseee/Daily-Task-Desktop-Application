using System.Windows;
using DailyTasks.Models;
using DailyTasks.Services;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

public sealed class TaskCoordinator(
    ITaskService tasks,
    ICategoryService categories,
    ISubtaskEditor subtaskEditor,
    IProjectExporter exporter,
    FocusService focus,
    SettingsService settings) : ITaskCoordinator
{
    public async Task<bool> CreateTaskAsync()
    {
        var categoryList = await categories.GetAllAsync();
        if (categoryList.Count == 0)
        {
            return false;
        }

        var window = new NewTaskWindow(categoryList, categoryList[0]) { Owner = Application.Current.MainWindow };
        if (window.ShowDialog() != true)
        {
            return false;
        }

        var task = new TaskItem
        {
            Title = window.TaskTitle,
            CategoryId = window.SelectedCategory.Id,
            Category = window.SelectedCategory,
            Priority = window.Priority,
            DueDate = window.DueDate,
        };

        await tasks.AddAsync(task); // raises TaskAdded so an open Today page shows it
        return true;
    }

    public async Task<int?> CreateProjectAsync()
    {
        var category = (await categories.GetAllAsync()).FirstOrDefault();
        if (category is null)
        {
            return null;
        }

        var window = new OrganizeWindow(null, askTitle: true) { Owner = Application.Current.MainWindow };
        if (window.ShowDialog() != true)
        {
            return null;
        }

        var task = new TaskItem
        {
            Title = window.ProjectTitle,
            CategoryId = category.Id,
            Category = category,
        };

        await tasks.AddAsync(task);
        await tasks.OrganizeAsync(task, window.Methodology, window.CustomPhases, window.IterationCount);
        await OpenDetailAsync(task.Id);
        return task.Id;
    }


    public async Task<bool> OrganizeAsync(TaskItem task)
    {
        var window = new OrganizeWindow(task.Methodology) { Owner = Application.Current.MainWindow };

        if (window.ShowDialog() != true)
        {
            return false;
        }

        if (window.Remove == true)
        {
            await tasks.ClearMethodologyAsync(task);
        }
        else
        {
            await tasks.OrganizeAsync(task, window.Methodology, window.CustomPhases, window.IterationCount);
        }

        return true;
    }

    public async Task OpenDetailAsync(int taskId)
    {
        var head = await tasks.GetAsync(taskId);

        if (head is null || head.Methodology is null)
        {
            return;
        }

        var viewModel = new TaskDetailViewModel(head, tasks, subtaskEditor, exporter, focus, settings.DeveloperFeaturesEnabled);
        var window = new TaskDetailWindow(viewModel) { Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }
}
