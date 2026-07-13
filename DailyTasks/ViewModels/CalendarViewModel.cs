using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyTasks.Models;
using DailyTasks.Services;

namespace DailyTasks.ViewModels;

/// <summary>One task/activity chip placed on a calendar day.</summary>
public sealed class CalendarItem
{
    public required int TaskId { get; init; }

    public required string Title { get; init; }

    public WorkStatus Status { get; init; }

    public required string ColorHex { get; init; }

    /// <summary>True for a methodology subtask (an "activity"); false for a standalone task.</summary>
    public bool IsActivity { get; init; }

    public bool IsDone => Status == WorkStatus.Done;

    public string Tooltip { get; init; } = string.Empty;
}

/// <summary>One cell in the month grid.</summary>
public sealed class CalendarDay
{
    public required DateTime Date { get; init; }

    public int DayNumber => Date.Day;

    public bool InMonth { get; init; }

    public bool IsToday { get; init; }

    public IReadOnlyList<CalendarItem> Items { get; init; } = [];

    /// <summary>Items shown before the "+N more" note.</summary>
    public IReadOnlyList<CalendarItem> Visible { get; init; } = [];

    public bool HasOverflow => Items.Count > Visible.Count;

    public int OverflowCount => Items.Count - Visible.Count;
}

/// <summary>
/// A month calendar of every dated task and activity, so work can be tracked at a glance. Each
/// task is placed on its due date (or start date when it has no due). Loads the whole forest
/// once; navigating months just re-lays the grid.
/// </summary>
public partial class CalendarViewModel(ITaskService tasks) : ObservableObject
{
    private const int MaxPerDay = 3;

    private List<(TaskItem Task, DateTime Date)> _dated = [];
    private DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty]
    private string _monthLabel = string.Empty;

    [ObservableProperty]
    private bool _isEmpty;

    public ObservableCollection<CalendarDay> Days { get; } = [];

    public IReadOnlyList<string> WeekdayHeaders { get; } =
        ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

    public async Task LoadAsync()
    {
        var roots = await tasks.GetRootsAsync();

        _dated = roots
            .SelectMany(r => TaskTree.Descendants(r).Prepend(r))
            .Select(t => (Task: t, Date: (t.DueDate ?? t.StartDate)?.Date))
            .Where(x => x.Date is not null)
            .Select(x => (x.Task, x.Date!.Value))
            .ToList();

        IsEmpty = _dated.Count == 0;
        BuildGrid();
    }

    [RelayCommand]
    private void PreviousMonth()
    {
        _month = _month.AddMonths(-1);
        BuildGrid();
    }

    [RelayCommand]
    private void NextMonth()
    {
        _month = _month.AddMonths(1);
        BuildGrid();
    }

    [RelayCommand]
    private void GoToday()
    {
        _month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        BuildGrid();
    }

    private void BuildGrid()
    {
        MonthLabel = _month.ToString("MMMM yyyy");

        var byDate = _dated
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Task.Priority).ThenBy(x => x.Task.Title).ToList());

        // Start the grid on the Sunday on/before the 1st; always show six weeks.
        var gridStart = _month.AddDays(-(int)_month.DayOfWeek);

        Days.Clear();
        for (var i = 0; i < 42; i++)
        {
            var date = gridStart.AddDays(i);
            var items = byDate.TryGetValue(date, out var hits)
                ? hits.Select(h => ToItem(h.Task)).ToList()
                : [];

            Days.Add(new CalendarDay
            {
                Date = date,
                InMonth = date.Month == _month.Month,
                IsToday = date == DateTime.Today,
                Items = items,
                Visible = items.Take(MaxPerDay).ToList(),
            });
        }
    }

    private static CalendarItem ToItem(TaskItem task)
    {
        var isActivity = task.ParentTaskId is not null;
        var due = task.DueDate ?? task.StartDate;

        return new CalendarItem
        {
            TaskId = task.Id,
            Title = task.Title,
            Status = task.Status,
            ColorHex = task.Category?.ColorHex ?? "#64748B",
            IsActivity = isActivity,
            Tooltip = $"{task.Title}\n{(isActivity ? "Activity" : "Task")} · {due:ddd d MMM} · {task.Status.Label()}",
        };
    }
}
