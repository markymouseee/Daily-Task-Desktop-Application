using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// The Projects hub: lists every project as a summary card and owns the "new project"
/// and "open detail" entry points via the coordinator.
/// </summary>
public partial class ProjectsViewModel(
    IProjectService projects,
    ITaskService tasks,
    IProjectCoordinator coordinator) : ObservableObject
{
    [ObservableProperty]
    private bool _isEmpty = true;

    public ObservableCollection<ProjectViewModel> Projects { get; } = [];

    public async Task LoadAsync()
    {
        Projects.Clear();

        foreach (var project in await projects.GetAllAsync())
        {
            Projects.Add(new ProjectViewModel(project));
        }

        IsEmpty = Projects.Count == 0;
    }

    [RelayCommand]
    private async Task NewProjectAsync()
    {
        var created = await coordinator.CreateAsync();

        if (created is not null)
        {
            Projects.Insert(0, new ProjectViewModel(created));
            IsEmpty = false;
        }
    }

    [RelayCommand]
    private async Task OpenAsync(ProjectViewModel project)
    {
        await coordinator.OpenDetailAsync(project.Id);

        // Progress and blocked counts may have shifted while the detail was open.
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(ProjectViewModel project)
    {
        // Deleting the heading task cascades to the project, phases and subtasks.
        await tasks.DeleteAsync(project.Model.TaskItem);
        Projects.Remove(project);
        IsEmpty = Projects.Count == 0;
    }
}
