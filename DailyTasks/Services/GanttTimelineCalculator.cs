namespace DailyTasks.Services;

/// <summary>Header/gridline granularity, tied to the zoom level.</summary>
public enum GanttColumnUnit
{
    Week,
    Month,
}

/// <summary>
/// The single source of date→pixel math for the Gantt chart: bar positions, the today
/// marker, and column header spacing all go through here so they stay in lock-step.
/// </summary>
public sealed class GanttTimelineCalculator
{
    private const double MinBarWidth = 6;

    public GanttTimelineCalculator(DateTime rangeStart, DateTime rangeEnd, double pixelsPerDay)
    {
        // Anchor to whole weeks so column headers line up at x = 0, 7·px, 14·px…
        RangeStart = StartOfWeek(rangeStart);
        RangeEnd = rangeEnd.Date < RangeStart ? RangeStart.AddDays(7) : rangeEnd.Date;
        PixelsPerDay = pixelsPerDay;
    }

    public DateTime RangeStart { get; }

    public DateTime RangeEnd { get; }

    public double PixelsPerDay { get; }

    public double TotalWidth => Math.Max(0, (RangeEnd - RangeStart).TotalDays) * PixelsPerDay;

    /// <summary>Pixels from the left edge for a given date.</summary>
    public double X(DateTime date) => (date.Date - RangeStart).TotalDays * PixelsPerDay;

    /// <summary>Bar width for a [start, end] span, clamped to a visible minimum.</summary>
    public double Width(DateTime start, DateTime end) =>
        Math.Max(MinBarWidth, (end.Date - start.Date).TotalDays * PixelsPerDay);

    /// <summary>Is the given date inside the visible range?</summary>
    public bool Contains(DateTime date) => date.Date >= RangeStart && date.Date <= RangeEnd;

    /// <summary>Column headers/gridlines for the given unit.</summary>
    public IReadOnlyList<(string Label, double X)> Columns(GanttColumnUnit unit) =>
        unit == GanttColumnUnit.Month ? MonthColumns() : WeekColumns();

    /// <summary>Week-boundary column headers, e.g. ("Jul 1", 0), ("Jul 8", 112)…</summary>
    private IReadOnlyList<(string Label, double X)> WeekColumns()
    {
        var columns = new List<(string, double)>();

        for (var day = RangeStart; day <= RangeEnd; day = day.AddDays(7))
        {
            columns.Add((day.ToString("MMM d"), X(day)));
        }

        return columns;
    }

    /// <summary>First-of-month column headers, e.g. ("Jul", …), ("Aug", …).</summary>
    private IReadOnlyList<(string Label, double X)> MonthColumns()
    {
        var columns = new List<(string, double)>();

        var first = new DateTime(RangeStart.Year, RangeStart.Month, 1);
        if (first < RangeStart)
        {
            first = first.AddMonths(1);
        }

        for (var day = first; day <= RangeEnd; day = day.AddMonths(1))
        {
            columns.Add((day.ToString("MMM"), X(day)));
        }

        return columns;
    }

    /// <summary>The Monday on or before the given date.</summary>
    public static DateTime StartOfWeek(DateTime date)
    {
        var delta = ((int)date.Date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.Date.AddDays(-delta);
    }
}
