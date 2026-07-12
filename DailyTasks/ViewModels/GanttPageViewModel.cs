using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// The Gantt hub: pick any methodology-organized task and see its timeline. Clicking a bar
/// opens that task's detail window.
/// </summary>
public partial class GanttPageViewModel(ITaskService tasks, ITaskCoordinator coordinator) : ObservableObject
{
    [ObservableProperty]
    private TaskItem? _selectedProject;

    [ObservableProperty]
    private GanttViewModel? _gantt;

    [ObservableProperty]
    private bool _isEmpty = true;

    public ObservableCollection<TaskItem> Projects { get; } = [];

    public async Task LoadAsync()
    {
        Projects.Clear();

        var roots = await tasks.GetRootsAsync();
        var organized = roots
            .SelectMany(r => TaskTree.Descendants(r).Prepend(r))
            .Where(t => t.Methodology is not null)
            .OrderBy(t => t.Title);

        foreach (var task in organized)
        {
            Projects.Add(task);
        }

        IsEmpty = Projects.Count == 0;
        SelectedProject = Projects.FirstOrDefault();
    }

    [RelayCommand]
    private async Task NewProject()
    {
        var id = await coordinator.CreateProjectAsync();
        await LoadAsync();

        if (id is { } newId)
        {
            SelectedProject = Projects.FirstOrDefault(p => p.Id == newId) ?? SelectedProject;
        }
    }

    partial void OnSelectedProjectChanged(TaskItem? value) =>
        Gantt = value is null
            ? null
            : new GanttViewModel(value, taskId => { _ = coordinator.OpenDetailAsync(value.Id); });
}
