using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// The Projects page: every methodology-organized head, kept out of the plain task lists so a
/// project and a to-do never look or behave the same. Cards open into the detail window (phases /
/// Gantt / board / export) rather than being checked off inline.
/// </summary>
public partial class ProjectsViewModel(
    ITaskService tasks,
    ITaskCoordinator coordinator,
    IProjectExporter exporter) : ObservableObject, IProjectCardHost
{
    [ObservableProperty]
    private bool _isEmpty = true;

    [ObservableProperty]
    private bool _isExporting;

    public ObservableCollection<ProjectCardViewModel> Projects { get; } = [];

    public async Task LoadAsync()
    {
        Projects.Clear();

        var roots = await tasks.GetRootsAsync();

        var organized = roots
            .SelectMany(r => TaskTree.Descendants(r).Prepend(r))
            .Where(t => t.Methodology is not null)
            .OrderBy(t => t.Category.Name)
            .ThenBy(t => t.Title);

        foreach (var head in organized)
        {
            Projects.Add(new ProjectCardViewModel(head, this));
        }

        IsEmpty = Projects.Count == 0;
    }

    [RelayCommand]
    private async Task NewProject()
    {
        await coordinator.CreateProjectAsync();
        await LoadAsync();
    }

    // ---- IProjectCardHost ----

    public async Task OpenAsync(ProjectCardViewModel project)
    {
        await coordinator.OpenDetailAsync(project.Model.Id);
        await LoadAsync();
    }

    public async Task ExportAsync(ProjectCardViewModel project)
    {
        var path = exporter.PromptForPath(project.Model);
        if (path is null)
        {
            return;
        }

        IsExporting = true;
        try
        {
            await exporter.WriteAsync(project.Model, path);
        }
        finally
        {
            IsExporting = false;
        }
    }

    public async Task ChangeMethodologyAsync(ProjectCardViewModel project)
    {
        if (await coordinator.OrganizeAsync(project.Model))
        {
            await LoadAsync();
        }
    }

    public async Task DeleteAsync(ProjectCardViewModel project)
    {
        await tasks.DeleteAsync(project.Model);
        Projects.Remove(project);
        IsEmpty = Projects.Count == 0;
    }
}
