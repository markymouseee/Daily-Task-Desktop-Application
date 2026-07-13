using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>One stage in the DevOps pipeline (e.g. "Build"), with its rollup and highlight state.</summary>
public sealed class PipelineStageViewModel
{
    public required int Index { get; init; }

    public required string Name { get; init; }

    public int Total { get; init; }

    public int Done { get; init; }

    /// <summary>All tasks in the stage are done (and there is at least one).</summary>
    public bool IsDone { get; init; }

    /// <summary>The pipeline's active stage — the first not-yet-complete stage.</summary>
    public bool IsCurrent { get; init; }

    public string CountText => Total == 0 ? "—" : $"{Done}/{Total}";
}

/// <summary>
/// Projects a DevOps head's stage phases into a looping pipeline: Plan → Code → … → Monitor,
/// with the current stage highlighted. The loop back from Monitor to Plan is drawn by the view.
/// </summary>
public sealed class PipelineViewModel
{
    public PipelineViewModel(TaskItem head)
    {
        var ordered = head.Phases.OrderBy(p => p.Order).ToList();
        var byPhase = head.Children.ToLookup(c => c.PhaseId);
        var progresses = ordered.Select(p => Progress.Of(byPhase[p.Id])).ToList();

        // The active stage is the first that isn't fully complete; if every stage is complete,
        // the pipeline has cycled through, so highlight the last stage.
        var current = -1;
        for (var i = 0; i < progresses.Count; i++)
        {
            if (!progresses[i].IsComplete)
            {
                current = i;
                break;
            }
        }

        if (current == -1)
        {
            current = ordered.Count - 1;
        }

        Stages = ordered.Select((p, i) => new PipelineStageViewModel
        {
            Index = i,
            Name = p.Name,
            Total = progresses[i].Total,
            Done = progresses[i].Done,
            IsDone = progresses[i].IsComplete,
            IsCurrent = i == current,
        }).ToList();
    }

    public IReadOnlyList<PipelineStageViewModel> Stages { get; }
}
