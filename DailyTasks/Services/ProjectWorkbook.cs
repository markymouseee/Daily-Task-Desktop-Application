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

    public static void Save(Project project, string path)
    {
        using var workbook = new XLWorkbook();

        if (project.Methodology == Methodology.Iterative && project.IterationCount is > 0)
        {
            for (var iteration = 1; iteration <= project.IterationCount; iteration++)
            {
                var slice = project.Subtasks.Where(s => s.IterationNumber == iteration).ToList();
                BuildSheet(workbook, $"Iteration {iteration}", $"{project.TaskItem.Title} — Iteration {iteration}", project, slice);
            }

            var unassigned = project.Subtasks.Where(s => s.IterationNumber is null).ToList();
            if (unassigned.Count > 0)
            {
                BuildSheet(workbook, "Unassigned", $"{project.TaskItem.Title} — Unassigned", project, unassigned);
            }
        }
        else
        {
            BuildSheet(workbook, SheetName(project.TaskItem.Title), project.TaskItem.Title, project, project.Subtasks.ToList());
        }

        BuildGanttSheet(workbook, project);

        workbook.SaveAs(path);
    }

    private static void BuildSheet(XLWorkbook workbook, string sheetName, string title, Project project, IReadOnlyList<Subtask> subtasks)
    {
        var ws = workbook.Worksheets.Add(UniqueName(workbook, sheetName));

        // ---- title ----
        var titleCell = ws.Cell(1, 1);
        titleCell.Value = $"{title}  ·  {project.Methodology}";
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

        foreach (var (groupName, items) in GroupsFor(project, subtasks))
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

    private static int WriteGroup(IXLWorksheet ws, int row, string groupName, IReadOnlyList<Subtask> items)
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
    private static IEnumerable<(string Name, IReadOnlyList<Subtask> Items)> GroupsFor(Project project, IReadOnlyList<Subtask> subtasks)
    {
        var phases = project.Phases.OrderBy(p => p.Order).ToList();

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
        foreach (var status in new[] { SubtaskStatus.Todo, SubtaskStatus.InProgress, SubtaskStatus.Review, SubtaskStatus.Blocked, SubtaskStatus.Done })
        {
            var items = subtasks.Where(s => s.Status == status).ToList();
            if (items.Count > 0)
            {
                yield return (StatusText(status), items);
            }
        }
    }

    private static XLColor StatusFill(SubtaskStatus status) => status switch
    {
        SubtaskStatus.Done => XLColor.FromHtml("#C6EFCE"),
        SubtaskStatus.InProgress => XLColor.FromHtml("#FFEB9C"),
        SubtaskStatus.Review => XLColor.FromHtml("#BDD7EE"),
        SubtaskStatus.Blocked => XLColor.FromHtml("#FFC7CE"),
        _ => XLColor.FromHtml("#D9D9D9"),
    };

    private static string StatusText(SubtaskStatus status) => status switch
    {
        SubtaskStatus.Todo => "To Do",
        SubtaskStatus.InProgress => "In Progress",
        SubtaskStatus.Review => "Review",
        SubtaskStatus.Done => "Done",
        SubtaskStatus.Blocked => "Blocked",
        _ => status.ToString(),
    };

    private static string PriorityText(TaskPriority priority) => priority.ToString();

    private static string Trim(double v) => v.ToString("0.#");

    // ---- Gantt worksheet: label/data columns + a weekly colour-filled timeline ----

    private const int GanttFirstWeekColumn = 9; // Task … Actual Hours occupy 1–8

    private static void BuildGanttSheet(XLWorkbook workbook, Project project)
    {
        var ws = workbook.Worksheets.Add(UniqueName(workbook, "Gantt"));
        var isWaterfall = project.Methodology == Methodology.Waterfall;

        var spans = GanttSchedule.PhaseSpans(project);
        var byPhase = project.Subtasks.ToLookup(s => s.PhaseId);
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
        var title = ws.Cell(1, 1);
        title.Value = $"{project.TaskItem.Title}  ·  {project.Methodology} — Gantt";
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, lastColumn).Merge();

        // Header row.
        const int headerRow = 2;
        string[] labels = ["Task", "Assigned To", "Role", "Start Date", "Due Date", "Status", "Est. Hours", "Actual Hours"];
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

        var row = headerRow + 1;

        foreach (var span in spans)
        {
            var subs = byPhase[span.Phase.Id].ToList();
            var phaseStatus = GanttSchedule.AggregatePhaseStatus(subs);

            // Phase header row: bold + shaded across the label columns.
            ws.Cell(row, 1).Value = span.Phase.Name;
            ws.Range(row, 1, row, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            ws.Range(row, 1, row, 8).Style.Font.SetBold();
            ws.Range(row, 1, row, 8).Style.Font.FontColor = XLColor.FromHtml("#0F172A");

            WriteDate(ws.Cell(row, 4), span.Start);
            WriteDate(ws.Cell(row, 5), span.End);
            ws.Cell(row, 6).Value = StatusText(phaseStatus);

            var est = subs.Sum(s => s.EstimatedHours ?? 0);
            var act = subs.Sum(s => s.ActualHours ?? 0);
            if (est > 0) ws.Cell(row, 7).Value = est;
            if (act > 0) ws.Cell(row, 8).Value = act;

            FillTimeline(ws, row, weeks, span.Start, span.End, phaseStatus);
            row++;

            foreach (var s in GanttSchedule.OrderSubtasks(subs, span))
            {
                var (start, end) = GanttSchedule.SubtaskSpan(s, span, isWaterfall);

                ws.Cell(row, 1).Value = "    " + s.Title;
                ws.Cell(row, 2).Value = s.AssignedTo?.Name ?? string.Empty;
                ws.Cell(row, 3).Value = s.AssignedTo?.Role ?? string.Empty;
                WriteDate(ws.Cell(row, 4), start);
                WriteDate(ws.Cell(row, 5), end);

                var statusCell = ws.Cell(row, 6);
                statusCell.Value = StatusText(s.Status);
                statusCell.Style.Fill.BackgroundColor = StatusFill(s.Status);

                if (s.EstimatedHours is { } e) ws.Cell(row, 7).Value = e;
                if (s.ActualHours is { } a) ws.Cell(row, 8).Value = a;

                FillTimeline(ws, row, weeks, start, end, s.Status);
                row++;
            }
        }

        // Legend.
        row += 1;
        ws.Cell(row, 1).Value = "Legend";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;

        foreach (var (label, status) in new[]
        {
            ("Done", SubtaskStatus.Done),
            ("In Progress", SubtaskStatus.InProgress),
            ("Blocked", SubtaskStatus.Blocked),
            ("Upcoming", SubtaskStatus.Todo),
        })
        {
            ws.Cell(row, 1).Style.Fill.BackgroundColor = TimelineFill(status);
            ws.Cell(row, 2).Value = label;
            row++;
        }

        // Column widths + frozen label/data columns and header.
        ws.Column(1).Width = 30;
        ws.Column(2).Width = 16;
        ws.Column(3).Width = 16;
        ws.Column(4).Width = 12;
        ws.Column(5).Width = 12;
        ws.Column(6).Width = 13;
        ws.Column(7).Width = 10;
        ws.Column(8).Width = 11;
        for (var w = 0; w < weeks.Count; w++)
        {
            ws.Column(GanttFirstWeekColumn + w).Width = 3.2;
        }

        ws.SheetView.Freeze(headerRow, 8);
    }

    private static void FillTimeline(IXLWorksheet ws, int row, List<DateTime> weeks, DateTime? start, DateTime? end, SubtaskStatus status)
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
    private static XLColor TimelineFill(SubtaskStatus status) => status switch
    {
        SubtaskStatus.Done => XLColor.FromHtml("#22C55E"),
        SubtaskStatus.InProgress => XLColor.FromHtml("#3B82F6"),
        SubtaskStatus.Review => XLColor.FromHtml("#3B82F6"),
        SubtaskStatus.Blocked => XLColor.FromHtml("#EF4444"),
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
