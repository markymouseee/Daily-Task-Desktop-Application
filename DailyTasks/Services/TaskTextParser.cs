using System.Globalization;
using System.Text.RegularExpressions;
using DailyTasks.Models;

namespace DailyTasks.Services;

/// <summary>
/// The result of reading a quick-add line. <see cref="Title"/> has the recognised
/// date/time/priority words stripped out of it.
/// </summary>
public sealed record ParsedTask(string Title, DateTime? DueDate, TaskPriority? Priority)
{
    public bool HasHints => DueDate is not null || Priority is not null;

    /// <summary>One-line echo of what was understood, for the live preview.</summary>
    public string Summary
    {
        get
        {
            var parts = new List<string>();

            if (DueDate is { } due)
            {
                parts.Add(due.TimeOfDay == TimeSpan.Zero
                    ? due.ToString("ddd d MMM", CultureInfo.CurrentCulture)
                    : due.ToString("ddd d MMM, h:mm tt", CultureInfo.CurrentCulture));
            }

            if (Priority is { } priority)
            {
                parts.Add($"{priority} priority");
            }

            return string.Join(" · ", parts);
        }
    }
}

/// <summary>
/// Pulls a due date, time and priority out of a plain sentence, e.g.
/// "Call dentist tomorrow 3pm high priority".
/// </summary>
public static class TaskTextParser
{
    private const RegexOptions Opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly (Regex Pattern, TaskPriority Priority)[] PriorityRules =
    [
        (new Regex(@"\b(?:high|highest|urgent)\s+priority\b", Opts), TaskPriority.High),
        (new Regex(@"\b(?:medium|med|normal)\s+priority\b", Opts), TaskPriority.Medium),
        (new Regex(@"\blow\s+priority\b", Opts), TaskPriority.Low),
        (new Regex(@"\bpriority\s*[:=]?\s*(?:high|urgent)\b", Opts), TaskPriority.High),
        (new Regex(@"\bpriority\s*[:=]?\s*(?:medium|med)\b", Opts), TaskPriority.Medium),
        (new Regex(@"\bpriority\s*[:=]?\s*low\b", Opts), TaskPriority.Low),
        (new Regex(@"!(?:high|urgent)\b", Opts), TaskPriority.High),
        (new Regex(@"!(?:medium|med)\b", Opts), TaskPriority.Medium),
        (new Regex(@"!low\b", Opts), TaskPriority.Low),

        // Only as a trailing tag: "Review PR urgent" is a priority,
        // "Fix urgent care booking page" is a title.
        (new Regex(@"[\s,]+(?:urgent|asap)\s*$", Opts), TaskPriority.High),
    ];

    private static readonly Regex NoonRule = new(@"\bnoon\b", Opts);
    private static readonly Regex MidnightRule = new(@"\bmidnight\b", Opts);
    private static readonly Regex MeridiemTime = new(@"\b(?:at\s+)?(\d{1,2})(?::(\d{2}))?\s*(am|pm)\b", Opts);
    private static readonly Regex ClockTime = new(@"\b(?:at\s+)?(\d{1,2}):(\d{2})\b", Opts);

    private static readonly Regex Today = new(@"\btoday\b", Opts);
    private static readonly Regex Tonight = new(@"\btonight\b", Opts);
    private static readonly Regex Tomorrow = new(@"\b(?:tomorrow|tmr|tmrw)\b", Opts);
    private static readonly Regex NextWeek = new(@"\bnext\s+week\b", Opts);
    private static readonly Regex InN = new(@"\bin\s+(\d{1,3})\s+(day|days|week|weeks|month|months)\b", Opts);

    private static readonly Regex WeekdayFull =
        new(@"\b(?:on\s+|next\s+)?(monday|tuesday|wednesday|thursday|friday|saturday|sunday)\b", Opts);

    // Abbreviations only count after "on"/"next", so "buy sun cream" keeps its "sun".
    private static readonly Regex WeekdayShort =
        new(@"\b(?:on|next)\s+(mon|tue|tues|wed|thu|thur|thurs|fri|sat|sun)\b", Opts);

    private static readonly Regex DayMonth =
        new(@"\b(\d{1,2})(?:st|nd|rd|th)?\s+(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*\b", Opts);

    private static readonly Regex MonthDay =
        new(@"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)[a-z]*\s+(\d{1,2})(?:st|nd|rd|th)?\b", Opts);

    private static readonly Regex NumericDate = new(@"\b(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2,4}))?\b", Opts);

    private static readonly string[] Months =
        ["jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec"];

    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.CultureInvariant);
    private static readonly Regex DanglingWord = new(@"[\s,]+(?:at|on|by|due|@)$", Opts);

    public static ParsedTask Parse(string input) => Parse(input, DateTime.Now);

    public static ParsedTask Parse(string input, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new ParsedTask(string.Empty, null, null);
        }

        var text = input;

        var priority = TakePriority(ref text);
        var time = TakeTime(ref text);
        var (date, impliedTime) = TakeDate(ref text, now);

        var due = Combine(date, time ?? impliedTime, now);

        return new ParsedTask(CleanTitle(text), due, priority);
    }

    private static DateTime? Combine(DateTime? date, TimeSpan? time, DateTime now)
    {
        if (date is null && time is null)
        {
            return null;
        }

        var day = date ?? now.Date;
        return time is { } t ? day.Add(t) : day;
    }

    private static TaskPriority? TakePriority(ref string text)
    {
        foreach (var (pattern, priority) in PriorityRules)
        {
            if (TryTake(ref text, pattern, out _))
            {
                return priority;
            }
        }

        return null;
    }

    private static TimeSpan? TakeTime(ref string text)
    {
        if (TryTake(ref text, NoonRule, out _))
        {
            return new TimeSpan(12, 0, 0);
        }

        if (TryTake(ref text, MidnightRule, out _))
        {
            return TimeSpan.Zero;
        }

        if (MeridiemTime.Match(text) is { Success: true } m)
        {
            var hour = int.Parse(m.Groups[1].Value);
            var minute = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;

            if (hour is >= 1 and <= 12 && minute < 60)
            {
                var pm = m.Groups[3].Value.Equals("pm", StringComparison.OrdinalIgnoreCase);
                hour = hour % 12 + (pm ? 12 : 0);
                Blank(ref text, m);
                return new TimeSpan(hour, minute, 0);
            }
        }

        if (ClockTime.Match(text) is { Success: true } c)
        {
            var hour = int.Parse(c.Groups[1].Value);
            var minute = int.Parse(c.Groups[2].Value);

            if (hour < 24 && minute < 60)
            {
                Blank(ref text, c);
                return new TimeSpan(hour, minute, 0);
            }
        }

        return null;
    }

    private static (DateTime? Date, TimeSpan? ImpliedTime) TakeDate(ref string text, DateTime now)
    {
        if (TryTake(ref text, Tonight, out _))
        {
            return (now.Date, new TimeSpan(20, 0, 0));
        }

        if (TryTake(ref text, Today, out _))
        {
            return (now.Date, null);
        }

        if (TryTake(ref text, Tomorrow, out _))
        {
            return (now.Date.AddDays(1), null);
        }

        // Must run before the weekday rules so "next week" is not read as "next <weekday>".
        if (TryTake(ref text, NextWeek, out _))
        {
            return (now.Date.AddDays(7), null);
        }

        if (TryTake(ref text, InN, out var inN))
        {
            var count = int.Parse(inN.Groups[1].Value);
            var unit = inN.Groups[2].Value.ToLowerInvariant();

            return (unit[0] switch
            {
                'd' => now.Date.AddDays(count),
                'w' => now.Date.AddDays(7 * count),
                _ => now.Date.AddMonths(count),
            }, null);
        }

        if (TryTake(ref text, WeekdayFull, out var full))
        {
            return (NextWeekday(now, full.Groups[1].Value), null);
        }

        if (TryTake(ref text, WeekdayShort, out var shortDay))
        {
            return (NextWeekday(now, shortDay.Groups[1].Value), null);
        }

        if (TryTake(ref text, DayMonth, out var dm))
        {
            return (BuildDate(now, int.Parse(dm.Groups[1].Value), MonthNumber(dm.Groups[2].Value)), null);
        }

        if (TryTake(ref text, MonthDay, out var md))
        {
            return (BuildDate(now, int.Parse(md.Groups[2].Value), MonthNumber(md.Groups[1].Value)), null);
        }

        if (NumericDate.Match(text) is { Success: true } nd)
        {
            var month = int.Parse(nd.Groups[1].Value);
            var day = int.Parse(nd.Groups[2].Value);

            if (month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(now.Year, month))
            {
                var year = nd.Groups[3].Success ? NormaliseYear(int.Parse(nd.Groups[3].Value)) : (int?)null;
                Blank(ref text, nd);
                return (year is { } y ? new DateTime(y, month, day) : BuildDate(now, day, month), null);
            }
        }

        return (null, null);
    }

    /// <summary>The upcoming occurrence of that weekday; never today.</summary>
    private static DateTime NextWeekday(DateTime now, string name)
    {
        var target = name.ToLowerInvariant()[..3] switch
        {
            "mon" => DayOfWeek.Monday,
            "tue" => DayOfWeek.Tuesday,
            "wed" => DayOfWeek.Wednesday,
            "thu" => DayOfWeek.Thursday,
            "fri" => DayOfWeek.Friday,
            "sat" => DayOfWeek.Saturday,
            _ => DayOfWeek.Sunday,
        };

        var delta = ((int)target - (int)now.DayOfWeek + 7) % 7;
        return now.Date.AddDays(delta == 0 ? 7 : delta);
    }

    /// <summary>A bare "14 Jul" means the next 14 July, this year or next.</summary>
    private static DateTime BuildDate(DateTime now, int day, int month)
    {
        if (day < 1 || day > DateTime.DaysInMonth(now.Year, month))
        {
            return now.Date;
        }

        var candidate = new DateTime(now.Year, month, day);
        return candidate < now.Date ? candidate.AddYears(1) : candidate;
    }

    private static int MonthNumber(string token) =>
        Array.IndexOf(Months, token.ToLowerInvariant()[..3]) + 1;

    private static int NormaliseYear(int year) => year < 100 ? 2000 + year : year;

    private static string CleanTitle(string text)
    {
        var title = Whitespace.Replace(text, " ").Trim(' ', ',', '-', ':');
        title = DanglingWord.Replace(title, string.Empty);
        return title.Trim();
    }

    private static bool TryTake(ref string text, Regex pattern, out Match match)
    {
        match = pattern.Match(text);

        if (!match.Success)
        {
            return false;
        }

        Blank(ref text, match);
        return true;
    }

    /// <summary>Replace with spaces rather than deleting, so later matches keep their offsets.</summary>
    private static void Blank(ref string text, Match match) =>
        text = text.Remove(match.Index, match.Length)
                   .Insert(match.Index, new string(' ', match.Length));
}
