using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>
/// One row of the Agile Gantt: either a Sprint group header or a single activity. Activity rows
/// carry the full data table (assignee, dates, duration, status, %) plus the timeline bar.
/// </summary>
public sealed class AgileGanttRow
{
    public bool IsSprint { get; init; }

    public required string Sprint { get; init; }

    public required double Top { get; init; }

    // ---- activity data columns ----
    public int? TaskId { get; init; }

    /// <summary>The backing task, so an inline % edit can persist. Null on sprint headers.</summary>
    public TaskItem? Model { get; init; }

    /// <summary>A leaf activity's % is directly editable; a parent's is a read-only rollup.</summary>
    public bool IsLeaf { get; init; }

    public string Activity { get; init; } = string.Empty;

    public bool HasAssignee { get; init; }

    public string AssigneeName { get; init; } = string.Empty;

    public string AssigneeFirstName { get; init; } = string.Empty;

    public string AssigneeColor { get; init; } = "#64748B";

    public string StartText { get; init; } = string.Empty;

    public string EndText { get; init; } = string.Empty;

    public string DurationText { get; init; } = string.Empty;

    public WorkStatus Status { get; init; }

    public string StatusText { get; init; } = string.Empty;

    public int Percent { get; init; }

    public string PercentText { get; init; } = string.Empty;

    // ---- sprint header extras ----
    public string DateRangeText { get; init; } = string.Empty;

    // ---- timeline bar ----
    public bool HasBar { get; init; }

    public double BarLeft { get; init; }

    public double BarTop { get; init; }

    public double BarWidth { get; init; }

    public double FillWidth { get; init; }

    public double BarOpacity { get; init; } = 1;
}

/// <summary>A selectable "% done" value in the Gantt, backed by a concrete work status.</summary>
public sealed record StatusOption(string Label, WorkStatus Status);

/// <summary>
/// A real Gantt for the agile methodologies: activities grouped under their sprint, with a
/// Sprint / Activity / Assigned / Start / End / Duration / Status / % data table on the left and
/// a dated calendar timeline of status-coloured, %-filled bars on the right. All date→pixel math
/// goes through <see cref="GanttTimelineCalculator"/>; the view keeps the frozen table in sync
/// with the scrolling timeline.
/// </summary>
public partial class AgileGanttViewModel : ObservableObject
{
    public const double RowPitch = 34;
    public const double BarHeight = 18;
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
    private readonly Action<int>? _onActivate;
    private readonly Action<TaskItem>? _onEdited;
    private int _zoomIndex = 2;

    public AgileGanttViewModel(TaskItem head, Action<int>? onActivate = null, Action<TaskItem>? onEdited = null)
    {
        _head = head;
        _onActivate = onActivate;
        _onEdited = onEdited;
        Rebuild();
    }

    /// <summary>The % values a leaf activity can be set to inline, each mapped to a work status.</summary>
    public IReadOnlyList<StatusOption> StatusOptions { get; } =
    [
        new("0%", WorkStatus.Todo),
        new("50%", WorkStatus.InProgress),
        new("75%", WorkStatus.Review),
        new("100%", WorkStatus.Done),
        new("Blocked", WorkStatus.Blocked),
    ];

    /// <summary>Persists an inline % edit on a leaf activity and re-lays the chart so the bar and colour follow.</summary>
    public void SetStatus(AgileGanttRow row, WorkStatus status)
    {
        if (row.Model is not { } task || task.Status == status)
        {
            return;
        }

        task.Status = status;
        _onEdited?.Invoke(task);
        Rebuild();
    }

    public GanttTimelineCalculator Calculator { get; private set; } = null!;

    public IReadOnlyList<AgileGanttRow> Rows { get; private set; } = [];

    public IReadOnlyList<GanttColumn> Columns { get; private set; } = [];

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
    private void ActivateRow(AgileGanttRow? row)
    {
        if (row?.TaskId is { } id)
        {
            _onActivate?.Invoke(id);
        }
    }

    private void Rebuild()
    {
        var (pixelsPerDay, unit, _) = ZoomLevels[_zoomIndex];

        var ordered = _head.Phases.OrderBy(p => p.Order).ToList();
        var byPhase = _head.Children.ToLookup(c => c.PhaseId);
        var sprintLength = Math.Max(1, _head.SprintLengthDays ?? TaskRules.DefaultSprintLengthDays);
        var baseStart = SprintBaseStart(_head);

        // Overall dated range across every child, padded like the main Gantt.
        var allDates = _head.Children
            .SelectMany(c => new[] { c.StartDate, c.DueDate })
            .Where(d => d.HasValue)
            .Select(d => d!.Value.Date)
            .ToList();

        var (rawStart, rawEnd) = allDates.Count > 0
            ? (allDates.Min().AddDays(-3), allDates.Max().AddDays(10))
            : (DateTime.Today.AddDays(-3), DateTime.Today.AddDays(56));

        Calculator = new GanttTimelineCalculator(rawStart, rawEnd, pixelsPerDay);

        var rows = new List<AgileGanttRow>();
        var sprintIndex = 0;

        foreach (var phase in ordered)
        {
            var isBacklog = phase.Order == 0;
            var members = GanttSchedule.OrderSubtasks(byPhase[phase.Id].ToList(), default).ToList();
            var progress = Progress.Of(members);

            var rangeText = isBacklog ? "Unscheduled" : SprintRange(baseStart, sprintIndex, sprintLength);
            if (!isBacklog)
            {
                sprintIndex++;
            }

            rows.Add(new AgileGanttRow
            {
                IsSprint = true,
                Sprint = phase.Name,
                Top = rows.Count * RowPitch,
                DateRangeText = rangeText,
                Percent = progress.Percent,
                PercentText = progress.Total == 0 ? string.Empty : $"{progress.Percent}%",
                StatusText = progress.Total == 0 ? "Empty" : $"{progress.Done}/{progress.Total} done",
            });

            foreach (var task in members)
            {
                rows.Add(BuildActivity(task, phase.Name, rows.Count * RowPitch));
            }
        }

        Rows = rows;
        Columns = Calculator.Columns(unit).Select(c => new GanttColumn { Label = c.Label, X = c.X }).ToList();
        BodyHeight = Math.Max(RowPitch, rows.Count * RowPitch);
        HasToday = Calculator.Contains(DateTime.Today);
        TodayX = Calculator.X(DateTime.Today);
        HasBars = rows.Any(r => r.HasBar);

        OnPropertyChanged(string.Empty);
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
    }

    private AgileGanttRow BuildActivity(TaskItem task, string sprint, double top)
    {
        var start = task.StartDate?.Date;
        var end = task.DueDate?.Date ?? start;
        if (start is null)
        {
            start = end;
        }

        if (start is not null && end is not null && start > end)
        {
            start = end;
        }

        var hasBar = start is not null && end is not null;
        var percent = PercentFor(task);
        var assignee = task.AssignedTo;

        var width = hasBar ? Calculator.Width(start!.Value, end!.Value) : 0;

        return new AgileGanttRow
        {
            IsSprint = false,
            Sprint = sprint,
            Top = top,
            TaskId = task.Id,
            Model = task,
            IsLeaf = task.Children.Count == 0,
            Activity = task.Title,
            HasAssignee = assignee is not null,
            AssigneeName = assignee?.Name ?? string.Empty,
            AssigneeFirstName = DisplayText.FirstName(assignee?.Name),
            AssigneeColor = assignee?.InitialsColorHex ?? "#64748B",
            StartText = start?.ToString("MMM d") ?? "—",
            EndText = end?.ToString("MMM d") ?? "—",
            DurationText = hasBar ? $"{(end!.Value - start!.Value).Days + 1}d" : "—",
            Status = task.Status,
            StatusText = task.Status.Label(),
            Percent = percent,
            PercentText = $"{percent}%",
            HasBar = hasBar,
            BarLeft = hasBar ? Calculator.X(start!.Value) : 0,
            BarTop = top + BarPad,
            BarWidth = width,
            FillWidth = width * percent / 100.0,
            BarOpacity = 1,
        };
    }

    /// <summary>Completion % for an activity: its rollup if it has children, else status-derived.</summary>
    private static int PercentFor(TaskItem task)
    {
        if (task.Children.Count > 0)
        {
            return Progress.Of(TaskTree.Descendants(task)).Percent;
        }

        return task.Status switch
        {
            WorkStatus.Done => 100,
            WorkStatus.Review => 75,
            WorkStatus.InProgress => 50,
            _ => 0,
        };
    }

    private static string SprintRange(DateTime baseStart, int index, int sprintLength)
    {
        var start = baseStart.AddDays((long)index * sprintLength);
        var end = start.AddDays(sprintLength - 1);
        return $"{start:MMM d} – {end:MMM d}";
    }

    private static DateTime SprintBaseStart(TaskItem head)
    {
        if (head.StartDate is { } start)
        {
            return start.Date;
        }

        var dates = head.Children
            .Select(c => c.StartDate ?? c.DueDate)
            .Where(d => d.HasValue)
            .Select(d => d!.Value.Date)
            .ToList();

        return dates.Count > 0 ? dates.Min() : DateTime.Today;
    }

}
