using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>One Gantt row, pre-computed into pixel coordinates.</summary>
public sealed class GanttBarRow
{
    public required string Label { get; init; }

    public required double Top { get; init; }

    public bool IsPhase { get; init; }

    public int? TaskId { get; init; }

    public double Indent { get; init; }

    public bool HasBar { get; init; }

    public double BarLeft { get; init; }

    public double BarTop { get; init; }

    public double BarWidth { get; init; }

    public WorkStatus Status { get; init; }

    public double BarOpacity { get; init; } = 1;

    public bool HasBlocked { get; init; }

    public double BlockedWidth { get; init; }

    public bool HasAssignee { get; init; }

    public string AssigneeName { get; init; } = string.Empty;

    public string AssigneeFirstName { get; init; } = string.Empty;

    public string AssigneeColor { get; init; } = "#64748B";
}

public sealed class GanttColumn
{
    public required string Label { get; init; }

    public required double X { get; init; }
}

public sealed class GanttConnector
{
    public required PointCollection Points { get; init; }
}

/// <summary>
/// Turns a methodology-organized task's phases and child tasks into one continuous set of
/// Gantt rows plus timeline chrome (columns, today marker, dependency connectors). Owns the
/// zoom state; clicking a subtask bar calls back to sync the List view.
/// </summary>
public partial class GanttViewModel : ObservableObject
{
    public const double RowPitch = 34;
    public const double BarHeight = 20;
    public const double NameColumnWidth = 220;
    public const double AssignedColumnWidth = 132;
    public const double HeaderHeight = 30;

    private const double BarPad = (RowPitch - BarHeight) / 2;

    private static readonly (double PixelsPerDay, GanttColumnUnit Unit, string Name)[] ZoomLevels =
    [
        (5, GanttColumnUnit.Month, "Month"),
        (12, GanttColumnUnit.Week, "2 Weeks"),
        (22, GanttColumnUnit.Week, "Week"),
        (44, GanttColumnUnit.Week, "Day"),
    ];

    private readonly TaskItem _head;
    private readonly bool _isWaterfall;
    private readonly Action<int>? _onTaskActivated;

    private int _zoomIndex = 2;

    public GanttViewModel(TaskItem head, Action<int>? onTaskActivated = null)
    {
        _head = head;
        _isWaterfall = head.Methodology == Methodology.Waterfall;
        _onTaskActivated = onTaskActivated;
        Rebuild();
    }

    public GanttTimelineCalculator Calculator { get; private set; } = null!;

    public IReadOnlyList<GanttBarRow> Rows { get; private set; } = [];

    public IReadOnlyList<GanttColumn> Columns { get; private set; } = [];

    public IReadOnlyList<GanttConnector> Connectors { get; private set; } = [];

    public double TimelineWidth => Calculator.TotalWidth;

    public double BodyHeight { get; private set; }

    public bool HasToday { get; private set; }

    public double TodayX { get; private set; }

    public bool HasBars { get; private set; }

    public string ZoomLabel => ZoomLevels[_zoomIndex].Name;

    public bool CanZoomIn => _zoomIndex < ZoomLevels.Length - 1;

    public bool CanZoomOut => _zoomIndex > 0;

    [RelayCommand(CanExecute = nameof(CanZoomIn))]
    public void ZoomIn()
    {
        if (CanZoomIn)
        {
            _zoomIndex++;
            Rebuild();
        }
    }

    [RelayCommand(CanExecute = nameof(CanZoomOut))]
    public void ZoomOut()
    {
        if (CanZoomOut)
        {
            _zoomIndex--;
            Rebuild();
        }
    }

    [RelayCommand]
    private void ActivateRow(GanttBarRow? row)
    {
        if (row?.TaskId is { } id)
        {
            _onTaskActivated?.Invoke(id);
        }
    }

    private void Rebuild()
    {
        var (pixelsPerDay, unit, _) = ZoomLevels[_zoomIndex];

        var spans = GanttSchedule.PhaseSpans(_head);
        var range = GanttSchedule.DateRange(spans);

        var (rawStart, rawEnd) = range is { } r
            ? (r.Start.AddDays(-3), r.End.AddDays(10))
            : (DateTime.Today.AddDays(-3), DateTime.Today.AddDays(56));

        Calculator = new GanttTimelineCalculator(rawStart, rawEnd, pixelsPerDay);

        var byPhase = _head.Children.ToLookup(c => c.PhaseId);
        var rows = new List<GanttBarRow>();

        foreach (var span in spans)
        {
            var members = byPhase[span.Phase.Id].ToList();
            rows.Add(BuildPhaseRow(span, rows.Count * RowPitch, members));

            foreach (var child in GanttSchedule.OrderSubtasks(members, span))
            {
                rows.Add(BuildTaskRow(child, rows.Count * RowPitch, span));
            }
        }

        Rows = rows;
        Columns = Calculator.Columns(unit).Select(c => new GanttColumn { Label = c.Label, X = c.X }).ToList();
        Connectors = _isWaterfall ? BuildConnectors(rows) : [];
        BodyHeight = Math.Max(RowPitch, rows.Count * RowPitch);
        HasToday = Calculator.Contains(DateTime.Today);
        TodayX = Calculator.X(DateTime.Today);
        HasBars = rows.Any(r => r.HasBar);

        OnPropertyChanged(string.Empty);
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
    }

    private GanttBarRow BuildPhaseRow(PhaseSpan span, double top, List<TaskItem> members)
    {
        var progress = Progress.Of(members);

        if (!span.HasBar)
        {
            return new GanttBarRow
            {
                Label = span.Phase.Name,
                Top = top,
                IsPhase = true,
                Status = GanttSchedule.AggregatePhaseStatus(members),
            };
        }

        var left = Calculator.X(span.Start!.Value);
        var width = Calculator.Width(span.Start.Value, span.End!.Value);
        var blockedFraction = progress.Total == 0 ? 0 : (double)progress.Blocked / progress.Total;

        return new GanttBarRow
        {
            Label = span.Phase.Name,
            Top = top,
            IsPhase = true,
            HasBar = true,
            BarLeft = left,
            BarTop = top + BarPad,
            BarWidth = width,
            Status = GanttSchedule.AggregatePhaseStatus(members),
            BarOpacity = span.Phase.IsLocked ? 0.45 : 1,
            HasBlocked = progress.Blocked > 0,
            BlockedWidth = width * blockedFraction,
        };
    }

    private GanttBarRow BuildTaskRow(TaskItem task, double top, PhaseSpan phaseSpan)
    {
        var (start, end) = GanttSchedule.SubtaskSpan(task, phaseSpan, _isWaterfall);
        var assignee = task.AssignedTo;
        var displayStatus = task.Status == WorkStatus.Review ? WorkStatus.InProgress : task.Status;
        var hasBar = start is not null && end is not null;

        return new GanttBarRow
        {
            Label = task.Title,
            Top = top,
            TaskId = task.Id,
            Indent = 22,
            Status = displayStatus,
            BarOpacity = phaseSpan.Phase.IsLocked ? 0.45 : 1,
            HasBar = hasBar,
            BarLeft = hasBar ? Calculator.X(start!.Value) : 0,
            BarTop = top + BarPad,
            BarWidth = hasBar ? Calculator.Width(start!.Value, end!.Value) : 0,
            HasAssignee = assignee is not null,
            AssigneeName = assignee?.Name ?? string.Empty,
            AssigneeFirstName = assignee?.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty,
            AssigneeColor = assignee?.InitialsColorHex ?? "#64748B",
        };
    }

    private IReadOnlyList<GanttConnector> BuildConnectors(List<GanttBarRow> rows)
    {
        var phaseBars = rows.Where(r => r is { IsPhase: true, HasBar: true }).ToList();
        var connectors = new List<GanttConnector>();

        for (var i = 0; i < phaseBars.Count - 1; i++)
        {
            var a = phaseBars[i];
            var b = phaseBars[i + 1];

            var x1 = a.BarLeft + a.BarWidth;
            var y1 = a.BarTop + BarHeight / 2;
            var x2 = b.BarLeft;
            var y2 = b.BarTop + BarHeight / 2;
            var stub = x1 + 8;

            connectors.Add(new GanttConnector
            {
                Points = [new(x1, y1), new(stub, y1), new(stub, y2), new(x2, y2)],
            });
        }

        return connectors;
    }
}
