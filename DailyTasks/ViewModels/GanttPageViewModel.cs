using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// The standalone Gantt page: pick a project and see its timeline. Clicking a bar opens
/// that project's detail. A thin shell over the shared <see cref="GanttViewModel"/>.
/// </summary>
public partial class GanttPageViewModel(IProjectService projects, IProjectCoordinator coordinator) : ObservableObject
{
    [ObservableProperty]
    private Project? _selectedProject;

    [ObservableProperty]
    private GanttViewModel? _gantt;

    [ObservableProperty]
    private bool _isEmpty = true;

    public ObservableCollection<Project> Projects { get; } = [];

    public async Task LoadAsync()
    {
        var current = SelectedProject?.Id;

        Projects.Clear();
        foreach (var project in await projects.GetAllAsync())
        {
            Projects.Add(project);
        }

        IsEmpty = Projects.Count == 0;
        SelectedProject = Projects.FirstOrDefault(p => p.Id == current) ?? Projects.FirstOrDefault();
    }

    partial void OnSelectedProjectChanged(Project? value) =>
        Gantt = value is null ? null : new GanttViewModel(value, _ => OpenDetail(value.Id));

    private async void OpenDetail(int projectId)
    {
        await coordinator.OpenDetailAsync(projectId);
        await LoadAsync(); // dates/assignees may have changed
    }
}
