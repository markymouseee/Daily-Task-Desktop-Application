using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

public sealed record InterruptionRow(string Label, int Count, int MinutesLost, double BarWidth)
{
    public string Detail => MinutesLost > 0 ? $"{Count} × · {MinutesLost} min lost" : $"{Count} ×";
}

public sealed class HeatCell
{
    public DateTime Date { get; init; }

    public int Count { get; init; }

    public bool IsFuture { get; init; }

    public bool HasAny => Count > 0;

    public double Opacity { get; init; }

    public string Tooltip => $"{Date:ddd d MMM} — {Count} completed";
}

public sealed record CategorySlice(string Label, int Count, string ColorHex);

public partial class InsightsViewModel(ITaskService tasks, IInterruptionService interruptions) : ObservableObject
{
    private const double MaxBarWidth = 300;
    private const int HeatmapWeeks = 12;

    [ObservableProperty]
    private string _interruptionHeadline = string.Empty;

    [ObservableProperty]
    private bool _hasInterruptions;

    [ObservableProperty]
    private bool _hasCategoryData;

    public ObservableCollection<InterruptionRow> InterruptionRows { get; } = [];

    public ObservableCollection<HeatCell> HeatCells { get; } = [];

    public ObservableCollection<CategorySlice> CategorySlices { get; } = [];

    private static DateTime StartOfWeek(DateTime date) =>
        date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    public async Task LoadAsync()
    {
        var weekStart = StartOfWeek(DateTime.Today);

        await LoadInterruptionsAsync(weekStart);

        var all = await tasks.GetAllAsync();
        BuildHeatmap(all);
        BuildCategorySlices(all, weekStart);
    }

    private async Task LoadInterruptionsAsync(DateTime weekStart)
    {
        var events = await interruptions.GetSinceAsync(weekStart);

        var lost = events.Sum(e => e.MinutesLost);
        InterruptionHeadline = events.Count == 0
            ? "No interruptions logged this week. Start a focus session to begin tracking."
            : $"{events.Count} interruption{(events.Count == 1 ? "" : "s")} this week · about {lost} min lost";
        HasInterruptions = events.Count > 0;

        InterruptionRows.Clear();

        if (events.Count == 0)
        {
            return;
        }

        var groups = events
            .GroupBy(e => e.Reason)
            .Select(g => (Reason: g.Key, Count: g.Count(), Minutes: g.Sum(e => e.MinutesLost)))
            .OrderByDescending(g => g.Count)
            .ToList();

        var max = groups.Max(g => g.Count);

        foreach (var (reason, count, minutes) in groups)
        {
            InterruptionRows.Add(new InterruptionRow(
                reason.ToString(),
                count,
                minutes,
                Math.Max(8, MaxBarWidth * count / max)));
        }
    }

    private void BuildHeatmap(IReadOnlyList<TaskItem> all)
    {
        HeatCells.Clear();

        var start = StartOfWeek(DateTime.Today).AddDays(-7 * (HeatmapWeeks - 1));

        var byDay = all
            .Where(t => t.CompletedAt is not null)
            .GroupBy(t => t.CompletedAt!.Value.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        var max = byDay.Count > 0 ? byDay.Values.Max() : 1;

        // Row-major by weekday so a UniformGrid with 7 rows lays out GitHub-style:
        // one row per weekday, one column per week.
        for (var dow = 0; dow < 7; dow++)
        {
            for (var week = 0; week < HeatmapWeeks; week++)
            {
                var date = start.AddDays((week * 7) + dow);
                var count = byDay.GetValueOrDefault(date);

                HeatCells.Add(new HeatCell
                {
                    Date = date,
                    Count = count,
                    IsFuture = date > DateTime.Today,
                    Opacity = count == 0 ? 1 : 0.3 + (0.7 * count / max),
                });
            }
        }
    }

    private void BuildCategorySlices(IReadOnlyList<TaskItem> all, DateTime weekStart)
    {
        CategorySlices.Clear();

        // Group by value, not by Category instance — AsNoTracking materialises a
        // fresh Category object per row, so reference grouping never merges.
        var groups = all
            .Where(t => t.CompletedAt >= weekStart)
            .GroupBy(t => (t.Category.Name, t.Category.ColorHex))
            .OrderByDescending(g => g.Count());

        foreach (var group in groups)
        {
            CategorySlices.Add(new CategorySlice(group.Key.Name, group.Count(), group.Key.ColorHex));
        }

        HasCategoryData = CategorySlices.Count > 0;
    }
}
