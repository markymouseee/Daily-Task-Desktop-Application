using System.Windows.Threading;
using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>What the end-of-day recap has to say.</summary>
public sealed class RecapStats
{
    public int PlannedCount { get; init; }

    public int CompletedCount { get; init; }

    public string? SlippedCategory { get; init; }

    public int SlippedOpenCount { get; init; }

    public double EstimatedHours { get; init; }

    public double ActualHours { get; init; }

    public int InterruptionCount { get; init; }

    public int InterruptionMinutesLost { get; init; }
}

/// <summary>
/// Watches the clock and raises <see cref="RecapDue"/> once a day at the
/// user's recap time (or on launch, if the app opens after it).
/// </summary>
public sealed class RecapService(
    ITaskService tasks,
    IInterruptionService interruptions,
    SettingsService settings)
{
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromSeconds(30),
    };

    public event EventHandler<RecapStats>? RecapDue;

    public void Start()
    {
        _timer.Tick += async (_, _) => await CheckAsync();
        _timer.Start();

        // Opening the app after recap time still deserves today's recap.
        _ = CheckAsync();
    }

    public async Task CheckAsync()
    {
        var now = DateTime.Now;

        if (now.TimeOfDay < settings.RecapTime || settings.LastRecapDate?.Date == now.Date)
        {
            return;
        }

        // Claim the slot before the awaits so a second tick can't double-fire.
        settings.LastRecapDate = now.Date;

        RecapDue?.Invoke(this, await BuildStatsAsync(now.Date));
    }

    private async Task<RecapStats> BuildStatsAsync(DateTime today)
    {
        var roots = await tasks.GetRootsAsync();
        var all = roots.SelectMany(r => TaskTree.Descendants(r).Prepend(r)).ToList();

        var planned = all.Where(t => t.DueDate?.Date == today).ToList();
        var completedToday = all.Where(t => t.CompletedAt?.Date == today).ToList();

        var slipped = planned
            .Where(t => !t.IsCompleted)
            .GroupBy(t => t.Category.Name)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var events = await interruptions.GetSinceAsync(today);

        return new RecapStats
        {
            PlannedCount = planned.Count,
            CompletedCount = completedToday.Count,
            SlippedCategory = slipped?.Key,
            SlippedOpenCount = slipped?.Count() ?? 0,
            EstimatedHours = completedToday.Sum(t => t.EstimatedHours ?? 0),
            ActualHours = completedToday.Sum(t => t.ActualHours ?? 0),
            InterruptionCount = events.Count,
            InterruptionMinutesLost = events.Sum(i => i.MinutesLost),
        };
    }
}
