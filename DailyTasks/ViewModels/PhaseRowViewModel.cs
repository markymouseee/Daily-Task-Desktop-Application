using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// One phase section in the project detail view: its subtasks plus a mini progress
/// bar that shows completed (green) and blocked (red) proportions.
/// </summary>
public partial class PhaseRowViewModel : ObservableObject
{
    public PhaseRowViewModel(
        Phase phase,
        IEnumerable<SubtaskViewModel> subtasks,
        string? displayName = null,
        int? iterationNumber = null)
    {
        Phase = phase;
        DisplayName = displayName ?? phase.Name;
        IterationNumber = iterationNumber;

        foreach (var s in subtasks)
        {
            Subtasks.Add(s);
        }

        Recompute();
    }

    public Phase Phase { get; }

    public int Id => Phase.Id;

    public string Name => Phase.Name;

    /// <summary>The header text — the phase name, or "Iteration N · Phase" for Iterative.</summary>
    public string DisplayName { get; }

    /// <summary>Set only for Iterative sections; null otherwise.</summary>
    public int? IterationNumber { get; }

    public int Order => Phase.Order;

    public ObservableCollection<SubtaskViewModel> Subtasks { get; } = [];

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private double _doneFraction;

    [ObservableProperty]
    private double _blockedFraction;

    /// <summary>The untouched remainder of the bar (1 − done − blocked), never negative.</summary>
    [ObservableProperty]
    private double _remainingFraction;

    [ObservableProperty]
    private int _percent;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _isComplete;

    /// <summary>Recomputes the bar and lock state from the current subtask statuses.</summary>
    public void Recompute()
    {
        IsLocked = Phase.IsLocked;

        var progress = Progress.Of(Subtasks.Select(s => s.Model));

        DoneFraction = progress.Fraction;
        BlockedFraction = progress.Total == 0 ? 0 : (double)progress.Blocked / progress.Total;
        RemainingFraction = Math.Max(0, 1 - DoneFraction - BlockedFraction);
        Percent = progress.Percent;
        IsComplete = progress.IsComplete;

        ProgressText = progress.Total == 0
            ? "Empty"
            : $"{progress.Done}/{progress.Total} complete{(progress.HasBlocked ? $" · {progress.Blocked} blocked" : string.Empty)}";
    }
}
