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
    private VShapedGanttViewModel? _vGantt;

    [ObservableProperty]
    private CyclicalGanttViewModel? _cycleGantt;

    [ObservableProperty]
    private AgileGanttViewModel? _agileGantt;

    [ObservableProperty]
    private bool _isEmpty = true;

    public ObservableCollection<TaskItem> Projects { get; } = [];

    public bool ShowSequential => Gantt is not null;

    public bool ShowV => VGantt is not null;

    public bool ShowCyclical => CycleGantt is not null;

    public bool ShowAgileGantt => AgileGantt is not null;

    public async Task LoadAsync()
    {
        Projects.Clear();

        var roots = await tasks.GetRootsAsync();

        // Timeline-family methodologies show here (Gantt variants + sprint swimlanes). The
        // board/pipeline/flat methodologies have no timeline, so they're managed only in
        // their own detail window.
        var organized = roots
            .SelectMany(r => TaskTree.Descendants(r).Prepend(r))
            .Where(t => t.Methodology is { } m && IsTimelineFamily(m))
            .OrderBy(t => t.Title);

        foreach (var task in organized)
        {
            Projects.Add(task);
        }

        IsEmpty = Projects.Count == 0;
        SelectedProject = Projects.FirstOrDefault();
    }

    private static bool IsTimelineFamily(Methodology methodology) =>
        TaskRules.ChartTypeFor(methodology)
            is ChartType.SequentialGantt or ChartType.VShapedGantt or ChartType.CyclicalGantt or ChartType.AgileGantt;

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

    partial void OnSelectedProjectChanged(TaskItem? value)
    {
        Gantt = null;
        VGantt = null;
        CycleGantt = null;
        AgileGantt = null;

        if (value?.Methodology is { } methodology)
        {
            switch (TaskRules.ChartTypeFor(methodology))
            {
                case ChartType.VShapedGantt:
                    VGantt = new VShapedGanttViewModel(value);
                    break;
                case ChartType.CyclicalGantt:
                    CycleGantt = new CyclicalGanttViewModel(value);
                    break;
                case ChartType.AgileGantt:
                    AgileGantt = new AgileGanttViewModel(
                        value,
                        taskId => { _ = coordinator.OpenDetailAsync(value.Id); },
                        onEdited: task => { _ = tasks.UpdateAsync(task); });
                    break;
                default:
                    Gantt = new GanttViewModel(value, taskId => { _ = coordinator.OpenDetailAsync(value.Id); });
                    break;
            }
        }

        OnPropertyChanged(nameof(ShowSequential));
        OnPropertyChanged(nameof(ShowV));
        OnPropertyChanged(nameof(ShowCyclical));
        OnPropertyChanged(nameof(ShowAgileGantt));
    }
}
