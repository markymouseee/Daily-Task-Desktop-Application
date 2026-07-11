using System.Windows;
using DailyTasks.Models;
using DailyTasks.Services;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

/// <summary>
/// The WPF-facing implementation of <see cref="IProjectCoordinator"/>. Lives under
/// Views because it news up windows; view models depend only on the interface.
/// </summary>
public sealed class ProjectCoordinator(
    ICategoryService categories,
    ITaskService tasks,
    IProjectService projects,
    ISubtaskEditor subtaskEditor,
    IProjectExporter exporter,
    SettingsService settings) : IProjectCoordinator
{
    public async Task<Project?> CreateAsync()
    {
        var categoryList = await categories.GetAllAsync();

        var window = new NewProjectWindow(categoryList, categoryList.FirstOrDefault())
        {
            Owner = Application.Current.MainWindow,
        };

        if (window.ShowDialog() != true)
        {
            return null;
        }

        var task = new TaskItem
        {
            Title = window.ProjectTitle,
            CategoryId = window.SelectedCategory.Id,
            Category = window.SelectedCategory,
            Priority = window.Priority,
            DueDate = window.DueDate,
        };

        if (!window.IsProject)
        {
            // A plain checklist item; Today's TaskAdded subscription surfaces it.
            await tasks.AddAsync(task);
            return null;
        }

        return await projects.CreateProjectAsync(
            task,
            window.Methodology,
            window.CustomPhases,
            window.IterationCount);
    }

    public async Task OpenDetailAsync(int projectId)
    {
        var project = await projects.GetAsync(projectId);

        if (project is null)
        {
            return;
        }

        var viewModel = new ProjectDetailViewModel(
            project, projects, tasks, subtaskEditor, exporter, settings.DeveloperFeaturesEnabled);
        var window = new ProjectDetailWindow(viewModel) { Owner = Application.Current.MainWindow };

        window.ShowDialog();
    }
}
