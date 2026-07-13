using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>Actions a project card routes back to its owning page.</summary>
public interface IProjectCardHost
{
    Task OpenAsync(ProjectCardViewModel project);

    Task ExportAsync(ProjectCardViewModel project);

    Task ChangeMethodologyAsync(ProjectCardViewModel project);

    Task DeleteAsync(ProjectCardViewModel project);
}

/// <summary>
/// A methodology-organized project head, shown on the Projects page. Deliberately distinct from
/// <see cref="TaskItemViewModel"/>: a project isn't a checkable to-do — it's opened into its
/// phases/Gantt/board, so this card surfaces the methodology, rollup and phase count instead of a
/// checkbox and inline subtask adder.
/// </summary>
public partial class ProjectCardViewModel(TaskItem model, IProjectCardHost host) : ObservableObject
{
    public TaskItem Model { get; } = model;

    public string Title => Model.Title;

    public Methodology Methodology => Model.Methodology ?? Methodology.Waterfall;

    public string MethodologyBadge => TaskRules.DisplayName(Methodology);

    public string MethodologyBlurb => TaskRules.Description(Methodology);

    public string CategoryName => Model.Category.Name;

    public string CategoryColor => Model.Category.ColorHex;

    // ---- rollup ----

    private Progress Rollup => Progress.Of(TaskTree.Descendants(Model));

    public double ProgressFraction => Rollup.Fraction;

    public int PercentComplete => Rollup.Percent;

    public string ProgressText => $"{Rollup.Done}/{Rollup.Total} subtasks";

    public bool HasBlocked => Rollup.Blocked > 0;

    public string BlockedText => $"{Rollup.Blocked} blocked";

    public int PhaseCount => Model.Phases.Count;

    public string StructureText => PhaseCount > 0
        ? $"{PhaseCount} {(TaskRules.IsSprintBased(Methodology) ? "sprints/pools" : "phases")}"
        : "board";

    public bool IsCompleted => Rollup.Total > 0 && Rollup.IsComplete;

    /// <summary>Group header on the Projects page: active projects above completed ones.</summary>
    public string Group => IsCompleted ? "Completed" : "In progress";

    // ---- commands ----

    [RelayCommand]
    private Task Open() => host.OpenAsync(this);

    [RelayCommand]
    private Task Export() => host.ExportAsync(this);

    [RelayCommand]
    private Task Change() => host.ChangeMethodologyAsync(this);

    [RelayCommand]
    private Task Delete() => host.DeleteAsync(this);
}
