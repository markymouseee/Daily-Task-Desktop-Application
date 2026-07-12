using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// One phase section in the detail view: its child-task cards plus a mini progress bar
/// showing completed (green) and blocked (red) proportions.
/// </summary>
public partial class PhaseRowViewModel : ObservableObject
{
    public PhaseRowViewModel(Phase phase, IEnumerable<TaskItemViewModel> tasks, string? displayName = null, int? iterationNumber = null)
    {
        Phase = phase;
        DisplayName = displayName ?? phase.Name;
        IterationNumber = iterationNumber;

        foreach (var t in tasks)
        {
            Tasks.Add(t);
        }

        Recompute();
    }

    public Phase Phase { get; }

    public int Id => Phase.Id;

    public string Name => Phase.Name;

    public string DisplayName { get; }

    public int? IterationNumber { get; }

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = [];

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private double _doneFraction;

    [ObservableProperty]
    private double _blockedFraction;

    [ObservableProperty]
    private int _percent;

    [ObservableProperty]
    private string _progressText = string.Empty;

    public void Recompute()
    {
        IsLocked = Phase.IsLocked;

        var progress = Progress.Of(Tasks.Select(t => t.Model));

        DoneFraction = progress.Fraction;
        BlockedFraction = progress.Total == 0 ? 0 : (double)progress.Blocked / progress.Total;
        Percent = progress.Percent;

        ProgressText = progress.Total == 0
            ? "Empty"
            : $"{progress.Done}/{progress.Total} complete{(progress.HasBlocked ? $" · {progress.Blocked} blocked" : string.Empty)}";
    }
}
