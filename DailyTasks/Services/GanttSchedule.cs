using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>A phase's resolved timeline span. Start/End are null when it has no dated subtasks.</summary>
public readonly record struct PhaseSpan(Phase Phase, DateTime? Start, DateTime? End)
{
    public bool HasBar => Start is not null && End is not null;
}

/// <summary>
/// Derives phase-level timeline spans from a methodology head's child tasks. A phase's end
/// is the latest child due date; its start is the earliest explicit child start, or — for
/// Waterfall, when no start is given — the end of the previous dated phase, so sequential
/// work chains without every start being entered by hand.
/// </summary>
public static class GanttSchedule
{
    public static IReadOnlyList<PhaseSpan> PhaseSpans(TaskItem head)
    {
        var ordered = head.Phases.OrderBy(p => p.Order).ToList();
        var byPhase = head.Children.ToLookup(s => s.PhaseId);
        var waterfall = head.Methodology == Methodology.Waterfall;

        var spans = new List<PhaseSpan>(ordered.Count);
        DateTime? previousEnd = null;

        foreach (var phase in ordered)
        {
            var subs = byPhase[phase.Id];
            var starts = subs.Where(s => s.StartDate.HasValue).Select(s => s.StartDate!.Value.Date).ToList();
            var dues = subs.Where(s => s.DueDate.HasValue).Select(s => s.DueDate!.Value.Date).ToList();

            // A phase with no dated subtasks draws no bar and doesn't break the chain.
            if (starts.Count == 0 && dues.Count == 0)
            {
                spans.Add(new PhaseSpan(phase, null, null));
                continue;
            }

            var end = dues.Count > 0 ? dues.Max() : starts.Max();

            var start = starts.Count > 0
                ? starts.Min()
                : waterfall && previousEnd is { } chained
                    ? chained
                    : dues.Min();

            // Keep the bar well-formed even if the chained start lands past the end.
            if (start > end)
            {
                start = end;
            }

            spans.Add(new PhaseSpan(phase, start, end));
            previousEnd = end;
        }

        return spans;
    }

    /// <summary>
    /// A subtask's drawable [start, end]. End is its due (or start); start is its explicit
    /// start, or — for Waterfall — the phase's start, or its due as a last resort.
    /// </summary>
    public static (DateTime? Start, DateTime? End) SubtaskSpan(TaskItem subtask, PhaseSpan phaseSpan, bool isWaterfall)
    {
        var end = subtask.DueDate?.Date ?? subtask.StartDate?.Date;

        var start = subtask.StartDate?.Date
            ?? (isWaterfall ? phaseSpan.Start : null)
            ?? subtask.DueDate?.Date;

        if (start is not null && end is not null && start > end)
        {
            start = end;
        }

        return (start, end);
    }

    /// <summary>Child tasks in timeline order: earliest start (or due) first, then priority.</summary>
    public static IEnumerable<TaskItem> OrderSubtasks(IEnumerable<TaskItem> subtasks, PhaseSpan span) =>
        subtasks.OrderBy(s => s.StartDate ?? s.DueDate ?? span.Start ?? DateTime.MaxValue)
            .ThenByDescending(s => s.Priority);

    /// <summary>
    /// A phase's single status for its bar: Done when everything's done, In Progress once any
    /// work has started, otherwise Todo (Upcoming).
    /// </summary>
    public static WorkStatus AggregatePhaseStatus(IReadOnlyCollection<TaskItem> subtasks)
    {
        if (Progress.Of(subtasks).IsComplete)
        {
            return WorkStatus.Done;
        }

        return subtasks.Any(s => s.Status is WorkStatus.InProgress or WorkStatus.Review or WorkStatus.Done)
            ? WorkStatus.InProgress
            : WorkStatus.Todo;
    }

    /// <summary>
    /// The overall dated range across the spans, or null when nothing is dated.
    /// </summary>
    public static (DateTime Start, DateTime End)? DateRange(IEnumerable<PhaseSpan> spans)
    {
        var withBars = spans.Where(s => s.HasBar).ToList();

        if (withBars.Count == 0)
        {
            return null;
        }

        return (withBars.Min(s => s.Start!.Value), withBars.Max(s => s.End!.Value));
    }
}
