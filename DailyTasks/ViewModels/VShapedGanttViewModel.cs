using System.Windows.Media;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>One phase box on the V — a development phase (top row) or a testing phase (bottom).</summary>
public sealed class VPhaseBox
{
    public required string Name { get; init; }

    public required string RangeText { get; init; }

    public WorkStatus Status { get; init; }

    public int Percent { get; init; }

    public required string ProgressText { get; init; }

    public bool IsDev { get; init; }

    public double Left { get; init; }

    public double Top { get; init; }

    public double Width { get; init; }

    public double Height { get; init; }
}

/// <summary>A polyline linking a development phase box to its paired testing phase box.</summary>
public sealed class VConnector
{
    public required PointCollection Points { get; init; }
}

/// <summary>
/// Lays a V-Model head out as two rows of phase boxes — development phases along the top,
/// their paired testing phases along the bottom, shifted right to read as "later in time" —
/// with a connector line drawn from each development box down to its paired testing box.
/// </summary>
public sealed class VShapedGanttViewModel
{
    private const double BoxWidth = 158;
    private const double BoxHeight = 74;
    private const double Gap = 26;
    private const double Margin = 12;
    private const double TopY = 14;
    private const double BottomY = 168;

    public VShapedGanttViewModel(TaskItem head)
    {
        var ordered = head.Phases.OrderBy(p => p.Order).ToList();
        var byId = ordered.ToDictionary(p => p.Id);
        var byPhase = head.Children.ToLookup(c => c.PhaseId);
        var spans = GanttSchedule.PhaseSpans(head).ToDictionary(s => s.Phase.Id);

        // Development phases carry the pairing; each maps to one testing phase.
        var devPhases = ordered.Where(p => p.PairedPhaseId is not null).ToList();

        var boxes = new List<VPhaseBox>();
        var connectors = new List<VConnector>();

        for (var i = 0; i < devPhases.Count; i++)
        {
            var dev = devPhases[i];
            var devLeft = Margin + i * (BoxWidth + Gap);
            boxes.Add(Box(dev, byPhase, spans, devLeft, TopY, isDev: true));

            if (dev.PairedPhaseId is { } pairedId && byId.TryGetValue(pairedId, out var test))
            {
                // Shift the testing box half a column right so the connector angles forward.
                var testLeft = devLeft + (BoxWidth + Gap) / 2;
                boxes.Add(Box(test, byPhase, spans, testLeft, BottomY, isDev: false));

                var x1 = devLeft + BoxWidth / 2;
                var y1 = TopY + BoxHeight;
                var x2 = testLeft + BoxWidth / 2;
                var y2 = BottomY;
                var midY = (y1 + y2) / 2;

                connectors.Add(new VConnector
                {
                    Points = [new(x1, y1), new(x1, midY), new(x2, midY), new(x2, y2)],
                });
            }
        }

        Boxes = boxes;
        Connectors = connectors;
        CanvasWidth = boxes.Count == 0 ? 0 : boxes.Max(b => b.Left + b.Width) + Margin;
        CanvasHeight = BottomY + BoxHeight + Margin;
    }

    public IReadOnlyList<VPhaseBox> Boxes { get; }

    public IReadOnlyList<VConnector> Connectors { get; }

    public double CanvasWidth { get; }

    public double CanvasHeight { get; }

    private static VPhaseBox Box(
        Phase phase,
        ILookup<int?, TaskItem> byPhase,
        IReadOnlyDictionary<int, PhaseSpan> spans,
        double left,
        double top,
        bool isDev)
    {
        var members = byPhase[phase.Id].ToList();
        var progress = Progress.Of(members);

        var rangeText = spans.TryGetValue(phase.Id, out var span) && span.HasBar
            ? $"{span.Start:MMM d} – {span.End:MMM d}"
            : "No dates";

        return new VPhaseBox
        {
            Name = phase.Name,
            RangeText = rangeText,
            Status = GanttSchedule.AggregatePhaseStatus(members),
            Percent = progress.Percent,
            ProgressText = progress.Total == 0 ? "Empty" : $"{progress.Done}/{progress.Total}",
            IsDev = isDev,
            Left = left,
            Top = top,
            Width = BoxWidth,
            Height = BoxHeight,
        };
    }
}
