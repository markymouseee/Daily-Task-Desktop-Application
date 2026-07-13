using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>One phase row inside a cycle block (e.g. "Risk Analysis" within Cycle 2).</summary>
public sealed class CyclePhaseRow
{
    public required string Name { get; init; }

    public double DoneFraction { get; init; }

    public double BlockedFraction { get; init; }

    public int Percent { get; init; }

    public required string ProgressText { get; init; }
}

/// <summary>
/// One loop of the spiral/iteration — a bordered block containing the full mini phase
/// sequence for that cycle, labelled "Cycle N".
/// </summary>
public sealed class CycleBlock
{
    public required string Label { get; init; }

    public double DoneFraction { get; init; }

    public double BlockedFraction { get; init; }

    public required string ProgressText { get; init; }

    public required IReadOnlyList<CyclePhaseRow> Phases { get; init; }
}

/// <summary>
/// Projects a cyclical head (Spiral, Iterative &amp; Incremental, RAD) into repeating cycle
/// blocks, each holding its mini phase sequence, so the visualization reads as a series of
/// loops rather than one continuous bar.
/// </summary>
public sealed class CyclicalGanttViewModel
{
    public CyclicalGanttViewModel(TaskItem head)
    {
        var ordered = head.Phases.OrderBy(p => p.Order).ToList();
        var byPhase = head.Children.ToLookup(c => c.PhaseId);
        var cycles = Math.Max(1, head.IterationCount ?? 1);

        var blocks = new List<CycleBlock>();
        for (var n = 1; n <= cycles; n++)
        {
            var rows = new List<CyclePhaseRow>();
            var cycleMembers = new List<TaskItem>();

            foreach (var phase in ordered)
            {
                var members = byPhase[phase.Id].Where(c => c.IterationNumber == n).ToList();
                cycleMembers.AddRange(members);
                var progress = Progress.Of(members);

                rows.Add(new CyclePhaseRow
                {
                    Name = phase.Name,
                    DoneFraction = progress.Fraction,
                    BlockedFraction = progress.Total == 0 ? 0 : (double)progress.Blocked / progress.Total,
                    Percent = progress.Percent,
                    ProgressText = progress.Total == 0 ? "—" : $"{progress.Done}/{progress.Total}",
                });
            }

            var cycleProgress = Progress.Of(cycleMembers);
            blocks.Add(new CycleBlock
            {
                Label = $"Cycle {n}",
                DoneFraction = cycleProgress.Fraction,
                BlockedFraction = cycleProgress.Total == 0 ? 0 : (double)cycleProgress.Blocked / cycleProgress.Total,
                ProgressText = cycleProgress.Total == 0 ? "No tasks yet" : $"{cycleProgress.Done}/{cycleProgress.Total} done",
                Phases = rows,
            });
        }

        Cycles = blocks;
    }

    public IReadOnlyList<CycleBlock> Cycles { get; }
}
