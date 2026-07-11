using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// Wraps a <see cref="Project"/> for list and card rendering. The detail view adds
/// its own richer view models on top; this one carries the at-a-glance summary that
/// the Projects list and the Today card share.
/// </summary>
public partial class ProjectViewModel : ObservableObject
{
    public ProjectViewModel(Project model) => Model = model;

    public Project Model { get; }

    public int Id => Model.Id;

    public int TaskItemId => Model.TaskItemId;

    public string Title => Model.TaskItem.Title;

    public Methodology Methodology => Model.Methodology;

    /// <summary>The short tag shown on cards, e.g. "Agile".</summary>
    public string MethodologyBadge => Model.Methodology.ToString();

    public string CategoryName => Model.TaskItem.Category.Name;

    public string CategoryColor => Model.TaskItem.Category.ColorHex;

    public bool IsCompleted => Model.TaskItem.IsCompleted;

    public Progress Overall => Progress.Of(Model.Subtasks);

    public int PercentComplete => Overall.Percent;

    /// <summary>0–1, for binding a progress bar's value against a maximum of 1.</summary>
    public double ProgressFraction => Overall.Fraction;

    public string ProgressText => Overall.Total == 0
        ? "No subtasks yet"
        : $"{Overall.Done}/{Overall.Total} subtasks · {PercentComplete}%";

    public int BlockedCount => Overall.Blocked;

    public bool HasBlocked => Overall.Blocked > 0;

    public string BlockedText => $"{BlockedCount} blocked";

    /// <summary>
    /// The single subtask the user should pick up next: the highest-priority
    /// unfinished, unblocked item in the earliest unlocked phase. Null when the
    /// project has nothing actionable (all done, all blocked, or empty).
    /// </summary>
    public Subtask? CurrentActionable
    {
        get
        {
            bool Actionable(Subtask s) => s.Status is not (SubtaskStatus.Done or SubtaskStatus.Blocked);

            // Phased methodologies walk phases in order, skipping locked ones.
            var phases = Model.Phases.OrderBy(p => p.Order).ToList();

            if (phases.Count > 0)
            {
                foreach (var phase in phases.Where(p => !p.IsLocked))
                {
                    var next = Model.Subtasks
                        .Where(s => s.PhaseId == phase.Id && Actionable(s))
                        .OrderByDescending(s => s.Priority)
                        .ThenBy(s => s.DueDate ?? DateTime.MaxValue)
                        .FirstOrDefault();

                    if (next is not null)
                    {
                        return next;
                    }
                }
            }

            // Kanban (and any orphaned items) fall back to a flat priority order.
            return Model.Subtasks
                .Where(Actionable)
                .OrderByDescending(s => s.Priority)
                .ThenBy(s => s.DueDate ?? DateTime.MaxValue)
                .FirstOrDefault();
        }
    }

    public string CurrentActionableTitle => CurrentActionable?.Title ?? "Nothing left to do";

    public bool HasActionable => CurrentActionable is not null;

    /// <summary>Re-reads every wrapped property after the project graph changed.</summary>
    public void Refresh() => OnPropertyChanged(string.Empty);
}
