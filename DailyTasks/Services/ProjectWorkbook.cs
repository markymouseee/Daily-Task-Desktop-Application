using ClosedXML.Excel;
using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// Builds the SDLC Excel workbook for a project: a summary block, a phase-grouped table
/// with per-phase subtotals, and status colouring. Iterative projects get one worksheet
/// per iteration. Pure ClosedXML — no UI — so it can run on a background thread.
/// </summary>
public static class ProjectWorkbook
{
    private const int Columns = 9; // Phase … Notes/Why

    private static readonly string[] Headers =
        ["Phase", "Subtask", "Priority", "Status", "Due Date", "Est. Hours", "Actual Hours", "Blocked Reason", "Notes / Why"];

    public static void Save(TaskItem head, string path)
    {
        using var workbook = new XLWorkbook();

        var chart = head.Methodology is { } m ? TaskRules.ChartTypeFor(m) : ChartType.SequentialGantt;

        if (head.Methodology is { } cyclical && TaskRules.UsesCycles(cyclical) && head.IterationCount is > 0)
        {
            for (var iteration = 1; iteration <= head.IterationCount; iteration++)
            {
                var slice = head.Children.Where(s => s.IterationNumber == iteration).ToList();
                BuildSheet(workbook, $"Cycle {iteration}", $"{head.Title} — Cycle {iteration}", head, slice);
            }

            var unassigned = head.Children.Where(s => s.IterationNumber is null).ToList();
            if (unassigned.Count > 0)
            {
                BuildSheet(workbook, "Unassigned", $"{head.Title} — Unassigned", head, unassigned);
            }
        }
        else
        {
            BuildSheet(workbook, SheetName(head.Title), head.Title, head, head.Children.ToList());
        }

        // A dated Gantt worksheet makes sense for every timeline chart type (including the
        // sprint-grouped Agile Gantt). Board/flat methodologies have no meaningful timeline,
        // so they export the data table alone.
        if (chart is ChartType.SequentialGantt or ChartType.VShapedGantt or ChartType.CyclicalGantt or ChartType.AgileGantt)
        {
            BuildGanttSheet(workbook, head, chart);
        }

        workbook.SaveAs(path);
    }

    private static void BuildSheet(XLWorkbook workbook, string sheetName, string title, TaskItem head, IReadOnlyList<TaskItem> subtasks)
    {
        var ws = workbook.Worksheets.Add(UniqueName(workbook, sheetName));

        // ---- title ----
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = $"{title}  ·  {head.Methodology}";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;
        ws.Range(1, 1, 1, Columns).Merge();

        // ---- summary block ----
        var progress = Progress.Of(subtasks);
        var estimated = subtasks.Sum(s => s.EstimatedHours ?? 0);
        var actual = subtasks.Sum(s => s.ActualHours ?? 0);

        WriteSummary(ws, 3, "Overall complete", $"{progress.Percent}%  ({progress.Done}/{progress.Total} subtasks)");
        WriteSummary(ws, 4, "Estimated vs actual hours", $"{Trim(estimated)}h estimated  ·  {Trim(actual)}h actual");
        WriteSummary(ws, 5, "Blocked subtasks", progress.Blocked.ToString());

        // ---- table header ----
        var headerRow = 7;
        for (var c = 0; c < Columns; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = Headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        var row = headerRow + 1;

        foreach (var (groupName, items) in GroupsFor(head, subtasks))
        {
            row = WriteGroup(ws, row, groupName, items);
        }

        // ---- finishing touches ----
        ws.SheetView.FreezeRows(headerRow);
        ws.Range(headerRow, 1, Math.Max(headerRow, row - 1), Columns).SetAutoFilter();

        ws.Column(1).Width = 18;
        ws.Column(2).Width = 40;
        ws.Column(3).Width = 11;
        ws.Column(4).Width = 13;
        ws.Column(5).Width = 16;
        ws.Column(6).Width = 11;
        ws.Column(7).Width = 12;
        ws.Column(8).Width = 30;
        ws.Column(9).Width = 34;
    }

    private static void WriteSummary(IXLWorksheet ws, int row, string label, string value)
    {
        var labelCell = ws.Cell(row, 1);
        labelCell.Value = label;
        labelCell.Style.Font.Bold = true;
        ws.Cell(row, 2).Value = value;
        ws.Range(row, 2, row, Columns).Merge();
    }

    private static int WriteGroup(IXLWorksheet ws, int row, string groupName, IReadOnlyList<TaskItem> items)
    {
        // Section header spanning the table.
        var header = ws.Cell(row, 1);
        header.Value = groupName;
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        ws.Range(row, 1, row, Columns).Merge().Style.Fill.BackgroundColor = XLColor.FromHtml("#3B82F6");
        row++;

        foreach (var s in items)
        {
            ws.Cell(row, 1).Value = groupName;
            ws.Cell(row, 2).Value = s.Title;
            ws.Cell(row, 3).Value = PriorityText(s.Priority);

            var statusCell = ws.Cell(row, 4);
            statusCell.Value = StatusText(s.Status);
            statusCell.Style.Fill.BackgroundColor = StatusFill(s.Status);

            if (s.DueDate is { } due)
            {
                ws.Cell(row, 5).Value = due;
                ws.Cell(row, 5).Style.DateFormat.Format = "yyyy-mm-dd";
            }

            if (s.EstimatedHours is { } est)
            {
                ws.Cell(row, 6).Value = est;
            }

            if (s.ActualHours is { } act)
            {
                ws.Cell(row, 7).Value = act;
            }

            ws.Cell(row, 8).Value = s.BlockedReason ?? string.Empty;
            ws.Cell(row, 9).Value = s.WhyReason ?? string.Empty;
            row++;
        }

        // Per-group subtotal, e.g. "Design — 3/4 complete".
        var progress = Progress.Of(items);
        var subtotal = ws.Cell(row, 1);
        subtotal.Value = $"{groupName} — {progress.Done}/{progress.Total} complete";
        subtotal.Style.Font.Bold = true;
        subtotal.Style.Font.Italic = true;
        ws.Range(row, 1, row, Columns).Merge().Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        row++;

        // Spacer row between groups.
        return row + 1;
    }

    /// <summary>
    /// Phase-grouped for methodologies with phases; status-grouped for Kanban. Phased
    /// projects list every phase in order (even empty ones) to mirror the full SDLC.
    /// </summary>
    private static IEnumerable<(string Name, IReadOnlyList<TaskItem> Items)> GroupsFor(TaskItem head, IReadOnlyList<TaskItem> subtasks)
    {
        var phases = head.Phases.OrderBy(p => p.Order).ToList();

        if (phases.Count > 0)
        {
            foreach (var phase in phases)
            {
                yield return (phase.Name, subtasks.Where(s => s.PhaseId == phase.Id).ToList());
            }

            var unassigned = subtasks.Where(s => s.PhaseId is null).ToList();
            if (unassigned.Count > 0)
            {
                yield return ("Unassigned", unassigned);
            }

            yield break;
        }

        // Kanban: group by status, skipping empty columns.
        foreach (var status in new[] { WorkStatus.Todo, WorkStatus.InProgress, WorkStatus.Review, WorkStatus.Blocked, WorkStatus.Done })
        {
            var items = subtasks.Where(s => s.Status == status).ToList();
            if (items.Count > 0)
            {
                yield return (StatusText(status), items);
            }
        }
    }

    private static XLColor StatusFill(WorkStatus status) => status switch
    {
        WorkStatus.Done => XLColor.FromHtml("#C6EFCE"),
        WorkStatus.InProgress => XLColor.FromHtml("#FFEB9C"),
        WorkStatus.Review => XLColor.FromHtml("#BDD7EE"),
        WorkStatus.Blocked => XLColor.FromHtml("#FFC7CE"),
        _ => XLColor.FromHtml("#D9D9D9"),
    };

    private static string StatusText(WorkStatus status) => status switch
    {
        WorkStatus.Todo => "To Do",
        WorkStatus.InProgress => "In Progress",
        WorkStatus.Review => "Review",
        WorkStatus.Done => "Done",
        WorkStatus.Blocked => "Blocked",
        _ => status.ToString(),
    };

    private static string PriorityText(TaskPriority priority) => priority.ToString();

    private static string Trim(double v) => v.ToString("0.#");

    // ---- Gantt worksheet: label/data columns + a weekly colour-filled timeline ----

    private const int GanttFirstWeekColumn = 9; // Task … Actual Hours occupy 1–8

    private static void BuildGanttSheet(XLWorkbook workbook, TaskItem head, ChartType chart)
    {
        var ws = workbook.Worksheets.Add(UniqueName(workbook, "Gantt"));
        var isWaterfall = head.Methodology == Methodology.Waterfall;

        var spans = GanttSchedule.PhaseSpans(head);
        var spanByPhase = spans.ToDictionary(s => s.Phase.Id);
        var byPhase = head.Children.ToLookup(s => s.PhaseId);
        var range = GanttSchedule.DateRange(spans);

        var rangeStart = GanttTimelineCalculator.StartOfWeek(range?.Start ?? DateTime.Today);
        var rangeEnd = range?.End ?? DateTime.Today.AddDays(56);

        var weeks = new List<DateTime>();
        for (var day = rangeStart; day <= rangeEnd; day = day.AddDays(7))
        {
            weeks.Add(day);
        }

        if (weeks.Count == 0)
        {
            weeks.Add(rangeStart);
        }

        var lastColumn = GanttFirstWeekColumn - 1 + weeks.Count;

        // Title.
        var chartLabel = chart switch
        {
            ChartType.VShapedGantt => "V-Model Gantt",
            ChartType.CyclicalGantt => "Cyclical Gantt",
            ChartType.AgileGantt => "Agile Gantt",
            _ => "Gantt",
        };

        var title = ws.Cell(1, 1);
        title.Value = $"{head.Title}  ·  {TaskRules.DisplayName(head.Methodology ?? Methodology.Waterfall)} — {chartLabel}";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 15;
        title.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
        ws.Range(1, 1, 1, lastColumn).Merge();

        // Header row: Sprint/Phase · Activity · Assigned · Start · End · Duration · Status · % · calendar.
        const int headerRow = 2;
        var firstColName = chart == ChartType.AgileGantt ? "Sprint" : "Phase";
        string[] labels = [firstColName, "Activity", "Assigned To", "Start", "End", "Duration", "Status", "% Done"];
        for (var c = 0; c < labels.Length; c++)
        {
            GanttHeaderCell(ws.Cell(headerRow, c + 1), labels[c]);
        }

        for (var w = 0; w < weeks.Count; w++)
        {
            var cell = ws.Cell(headerRow, GanttFirstWeekColumn + w);
            GanttHeaderCell(cell, weeks[w].ToString("MMM d"));
            cell.Style.Alignment.TextRotation = 90; // keep the week columns narrow
        }

        var pairNames = VModelPairNames(head);
        var row = headerRow + 1;

        foreach (var (groupHeader, phases, filter) in GanttGroups(head, chart))
        {
            // Materialize phase rows; for the grouped charts, skip phases empty in this group.
            var blocks = phases
                .Select(p => (Phase: p, Subs: byPhase[p.Id].Where(filter).ToList()))
                .Where(b => chart == ChartType.SequentialGantt || b.Subs.Count > 0)
                .ToList();

            if (blocks.Count == 0)
            {
                continue;
            }

            // Group band (e.g. "Cycle 2", "Development") spanning label + timeline columns.
            if (groupHeader is { } header)
            {
                ws.Cell(row, 1).Value = header;
                var band = ws.Range(row, 1, row, lastColumn).Merge();
                band.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
                band.Style.Font.SetBold();
                band.Style.Font.FontColor = XLColor.White;
                row++;
            }

            foreach (var (phase, subs) in blocks)
            {
                var phaseStatus = GanttSchedule.AggregatePhaseStatus(subs);
                var progress = Progress.Of(subs);

                // A sequential phase keeps its chained span; grouped charts derive it from
                // this group's own subtasks so each cycle/arm reads independently.
                var (pStart, pEnd) = chart == ChartType.SequentialGantt && spanByPhase.TryGetValue(phase.Id, out var ps)
                    ? (ps.Start, ps.End)
                    : SpanOfSubs(subs);

                var phaseSpan = new PhaseSpan(phase, pStart, pEnd);

                // Phase / sprint header row: bold + shaded across the data columns.
                ws.Cell(row, 1).Value = chart == ChartType.VShapedGantt && pairNames.TryGetValue(phase.Id, out var pair)
                    ? $"{phase.Name}  ↔  {pair}"
                    : phase.Name;
                ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#DBEAFE");
                ws.Range(row, 1, row, 8).Style.Font.SetBold();
                ws.Range(row, 1, row, 8).Style.Font.FontColor = XLColor.FromHtml("#0F172A");

                WriteDate(ws.Cell(row, 4), pStart);
                WriteDate(ws.Cell(row, 5), pEnd);
                WriteDuration(ws.Cell(row, 6), pStart, pEnd);
                ws.Cell(row, 7).Value = StatusText(phaseStatus);
                WritePercent(ws.Cell(row, 8), progress.Fraction);

                FillTimeline(ws, row, weeks, pStart, pEnd, phaseStatus);
                row++;

                foreach (var s in GanttSchedule.OrderSubtasks(subs, phaseSpan))
                {
                    var (start, end) = GanttSchedule.SubtaskSpan(s, phaseSpan, isWaterfall);

                    ws.Cell(row, 1).Value = phase.Name;
                    ws.Cell(row, 2).Value = s.Title;
                    ws.Cell(row, 3).Value = s.AssignedTo?.Name ?? string.Empty;
                    WriteDate(ws.Cell(row, 4), start);
                    WriteDate(ws.Cell(row, 5), end);
                    WriteDuration(ws.Cell(row, 6), start, end);

                    var statusCell = ws.Cell(row, 7);
                    statusCell.Value = StatusText(s.Status);
                    statusCell.Style.Fill.BackgroundColor = StatusFill(s.Status);

                    WritePercent(ws.Cell(row, 8), PercentFractionFor(s));

                    FillTimeline(ws, row, weeks, start, end, s.Status);
                    row++;
                }
            }
        }

        // A hairline border around the whole table for a cleaner, modern look.
        if (row > headerRow + 1)
        {
            var table = ws.Range(headerRow, 1, row - 1, lastColumn);
            table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            table.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            table.Style.Border.OutsideBorderColor = XLColor.FromHtml("#94A3B8");
            table.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8F0");
        }

        // Legend.
        row += 1;
        ws.Cell(row, 1).Value = "Legend";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        foreach (var (label, status) in new[]
        {
            ("Done", WorkStatus.Done),
            ("In Progress", WorkStatus.InProgress),
            ("Blocked", WorkStatus.Blocked),
            ("Upcoming", WorkStatus.Todo),
        })
        {
            ws.Cell(row, 1).Style.Fill.BackgroundColor = TimelineFill(status);
            ws.Cell(row, 2).Value = label;
            row++;
        }

        // Column widths + frozen data columns and header.
        ws.Column(1).Width = 18; // Sprint / Phase
        ws.Column(2).Width = 36; // Activity
        ws.Column(3).Width = 18; // Assigned To
        ws.Column(4).Width = 12; // Start
        ws.Column(5).Width = 12; // End
        ws.Column(6).Width = 10; // Duration
        ws.Column(7).Width = 13; // Status
        ws.Column(8).Width = 9;  // % Done
        for (var w = 0; w < weeks.Count; w++)
        {
            ws.Column(GanttFirstWeekColumn + w).Width = 3.2;
        }

        ws.Row(1).Height = 22;
        ws.SheetView.Freeze(headerRow, 8);
    }

    /// <summary>Writes a whole-day inclusive duration ("Nd"), leaving the cell blank when undated.</summary>
    private static void WriteDuration(IXLCell cell, DateTime? start, DateTime? end)
    {
        if (start is { } s && end is { } e)
        {
            cell.Value = $"{(e.Date - s.Date).Days + 1}d";
        }
    }

    /// <summary>Writes a 0–1 fraction as a real Excel percentage cell.</summary>
    private static void WritePercent(IXLCell cell, double fraction)
    {
        cell.Value = Math.Clamp(fraction, 0, 1);
        cell.Style.NumberFormat.Format = "0%";
    }

    /// <summary>Completion fraction for an activity: its rollup if it has children, else status-derived.</summary>
    private static double PercentFractionFor(TaskItem task)
    {
        if (task.Children.Count > 0)
        {
            return Progress.Of(TaskTree.Descendants(task)).Fraction;
        }

        return task.Status switch
        {
            WorkStatus.Done => 1.0,
            WorkStatus.Review => 0.75,
            WorkStatus.InProgress => 0.5,
            _ => 0.0,
        };
    }

    /// <summary>
    /// The row groups for the Gantt sheet: one un-headed group for a sequential chart,
    /// Development/Testing arms for V-Model, and one "Cycle n" group per iteration for the
    /// cyclical charts. The filter selects which subtasks belong to the group.
    /// </summary>
    private static IEnumerable<(string? Header, IReadOnlyList<Phase> Phases, Func<TaskItem, bool> Filter)> GanttGroups(TaskItem head, ChartType chart)
    {
        var ordered = head.Phases.OrderBy(p => p.Order).ToList();

        switch (chart)
        {
            case ChartType.VShapedGantt:
                var dev = ordered.Where(p => p.PairedPhaseId is not null).ToList();
                var testIds = dev.Select(p => p.PairedPhaseId!.Value).ToHashSet();
                var test = ordered.Where(p => testIds.Contains(p.Id)).ToList();
                var other = ordered.Where(p => p.PairedPhaseId is null && !testIds.Contains(p.Id)).ToList();

                yield return ("Development", dev, static _ => true);
                yield return ("Testing", test, static _ => true);
                if (other.Count > 0)
                {
                    yield return ("Other", other, static _ => true);
                }

                break;

            case ChartType.CyclicalGantt:
                var cycles = Math.Max(1, head.IterationCount ?? 1);
                for (var n = 1; n <= cycles; n++)
                {
                    var cycle = n; // capture per iteration
                    yield return ($"Cycle {cycle}", ordered, c => c.IterationNumber == cycle);
                }

                yield return ("Unassigned", ordered, static c => c.IterationNumber is null);
                break;

            default:
                yield return (null, ordered, static _ => true);
                break;
        }
    }

    /// <summary>Phase id → its V-Model paired phase name, in both directions.</summary>
    private static IReadOnlyDictionary<int, string> VModelPairNames(TaskItem head)
    {
        var nameById = head.Phases.ToDictionary(p => p.Id, p => p.Name);
        var pairs = new Dictionary<int, string>();

        foreach (var p in head.Phases)
        {
            if (p.PairedPhaseId is { } pid && nameById.TryGetValue(pid, out var pairedName))
            {
                pairs[p.Id] = pairedName;
                pairs[pid] = p.Name;
            }
        }

        return pairs;
    }

    /// <summary>The [earliest, latest] dated span across a set of subtasks, or (null, null).</summary>
    private static (DateTime? Start, DateTime? End) SpanOfSubs(IReadOnlyList<TaskItem> subs)
    {
        var dated = subs.Where(s => s.StartDate.HasValue || s.DueDate.HasValue).ToList();
        if (dated.Count == 0)
        {
            return (null, null);
        }

        var start = dated.Min(s => (s.StartDate ?? s.DueDate)!.Value.Date);
        var end = dated.Max(s => (s.DueDate ?? s.StartDate)!.Value.Date);
        return (start, end < start ? start : end);
    }

    private static void FillTimeline(IXLWorksheet ws, int row, List<DateTime> weeks, DateTime? start, DateTime? end, WorkStatus status)
    {
        if (start is null || end is null)
        {
            return;
        }

        var fill = TimelineFill(status);

        for (var w = 0; w < weeks.Count; w++)
        {
            var weekStart = weeks[w];
            var weekEnd = weekStart.AddDays(6);

            if (weekStart <= end.Value.Date && weekEnd >= start.Value.Date)
            {
                ws.Cell(row, GanttFirstWeekColumn + w).Style.Fill.BackgroundColor = fill;
            }
        }
    }

    private static void WriteDate(IXLCell cell, DateTime? date)
    {
        if (date is { } d)
        {
            cell.Value = d;
            cell.Style.DateFormat.Format = "yyyy-mm-dd";
        }
    }

    private static void GanttHeaderCell(IXLCell cell, string text)
    {
        cell.Value = text;
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#334155");
        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    /// <summary>Vivid in-app status colours, so the week block reads as a real Gantt bar.</summary>
    private static XLColor TimelineFill(WorkStatus status) => status switch
    {
        WorkStatus.Done => XLColor.FromHtml("#22C55E"),
        WorkStatus.InProgress => XLColor.FromHtml("#3B82F6"),
        WorkStatus.Review => XLColor.FromHtml("#3B82F6"),
        WorkStatus.Blocked => XLColor.FromHtml("#EF4444"),
        _ => XLColor.FromHtml("#64748B"),
    };

    /// <summary>Excel sheet names cap at 31 chars and forbid []:*?/\.</summary>
    private static string SheetName(string title)
    {
        var cleaned = new string(title.Select(c => "[]:*?/\\".Contains(c) ? ' ' : c).ToArray()).Trim();
        if (cleaned.Length == 0)
        {
            cleaned = "Project";
        }

        return cleaned.Length <= 31 ? cleaned : cleaned[..31];
    }

    private static string UniqueName(XLWorkbook workbook, string desired)
    {
        var name = SheetName(desired);
        if (!workbook.Worksheets.Contains(name))
        {
            return name;
        }

        // Append a counter while staying inside the 31-char cap.
        for (var i = 2; i < 100; i++)
        {
            var suffix = $" ({i})";
            var trimmed = name.Length + suffix.Length <= 31 ? name : name[..(31 - suffix.Length)];
            var candidate = trimmed + suffix;
            if (!workbook.Worksheets.Contains(candidate))
            {
                return candidate;
            }
        }

        return name;
    }
}
