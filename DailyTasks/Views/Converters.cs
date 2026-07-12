using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DailyTasks.Models;

namespace DailyTasks.Views;

internal static class Palette
{
    public static readonly IReadOnlyDictionary<TaskPriority, Color> Priority =
        new Dictionary<TaskPriority, Color>
        {
            [TaskPriority.High] = Color.FromRgb(0xEF, 0x44, 0x44),
            [TaskPriority.Medium] = Color.FromRgb(0xF5, 0x9E, 0x0B),
            [TaskPriority.Low] = Color.FromRgb(0x63, 0x66, 0xF1),
        };

    public static SolidColorBrush Brush(Color color, byte alpha = 0xFF)
    {
        var brush = new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// Category colour, from its stored "#RRGGBB". Pass ConverterParameter="Fill"
/// for the translucent pill background.
/// </summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string hex || string.IsNullOrWhiteSpace(hex))
        {
            return Brushes.Transparent;
        }

        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var isFill = parameter as string == "Fill";
            return Palette.Brush(color, isFill ? (byte)0x2E : (byte)0xFF);
        }
        catch (FormatException)
        {
            return Brushes.Transparent;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Priority colour. Pass ConverterParameter="Fill" for the translucent pill background,
/// matching the category/status pill idiom.
/// </summary>
public sealed class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TaskPriority priority || !Palette.Priority.TryGetValue(priority, out var color))
        {
            return Brushes.Transparent;
        }

        return parameter as string == "Fill" ? Palette.Brush(color, 0x2E) : Palette.Brush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Renders a due date the way a person would say it: Today, Tomorrow, Overdue, or "Mon 14 Jul".
/// </summary>
public sealed class DueDateToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not DateTime due)
        {
            return string.Empty;
        }

        var days = (due.Date - DateTime.Today).Days;

        var day = days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            -1 => "Yesterday",
            < 0 => $"Overdue · {due:ddd d MMM}",
            _ => due.ToString("ddd d MMM", culture),
        };

        return due.TimeOfDay == TimeSpan.Zero ? day : $"{day}, {due.ToString("h:mm tt", culture)}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Overdue dates are tinted; everything else uses the ambient secondary text brush.</summary>
public sealed class DueDateIsOverdueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime due && due.Date < DateTime.Today;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is null ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Collapses on a null, empty or whitespace string.</summary>
public sealed class EmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible when the bound bool is false (inverse of BoolToVisibility).</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Logical negation, for binding an IsEnabled against a "busy" flag.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not true;
}

/// <summary>Visible only when the bound integer count is zero (for empty-state hints).</summary>
public sealed class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Turns an indent amount (double) into a left-inset <see cref="Thickness"/>, keeping the
/// row's fixed right/vertical padding. Used to nest Gantt subtask labels under their phase.
/// </summary>
public sealed class IndentToMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var indent = value is double d ? d : 0;
        return new Thickness(12 + indent, 0, 10, 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True when the bound nullable date falls on today — used to flag Big 3 cards.</summary>
public sealed class IsTodayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime date && date.Date == DateTime.Today;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible only when the bound integer count is greater than zero.</summary>
public sealed class PositiveToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int n && n > 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// Turns a 0–1 fraction into a star <see cref="GridLength"/>, so a progress bar built
/// from Grid columns sizes its segments proportionally.
/// </summary>
public sealed class FractionToStarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var fraction = value is double d ? Math.Clamp(d, 0, 1) : 0;
        return new GridLength(fraction, GridUnitType.Star);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// The completion tint for a progress bar: gray when nothing's done, blue while in
/// progress, green once complete. Pass the fraction (0–1).
/// </summary>
public sealed class ProgressToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Empty = Palette.Brush(Color.FromRgb(0x64, 0x74, 0x8B));
    private static readonly SolidColorBrush InProgress = Palette.Brush(Color.FromRgb(0x3B, 0x82, 0xF6));
    private static readonly SolidColorBrush Complete = Palette.Brush(Color.FromRgb(0x22, 0xC5, 0x5E));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var fraction = value is double d ? d : 0;

        return fraction >= 1 ? Complete
            : fraction > 0 ? InProgress
            : Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// [fraction, availableWidth] → fraction × width, in pixels. Used to size the coloured
/// segments of a progress bar reliably (unlike binding a ColumnDefinition's width, which
/// doesn't inherit DataContext).
/// </summary>
public sealed class FractionOfWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not double fraction || values[1] is not double width || width <= 0)
        {
            return 0d;
        }

        return Math.Max(0, Math.Clamp(fraction, 0, 1) * width);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Fill colour for a task's status pill and its Kanban card accent.</summary>
public sealed class SubtaskStatusToBrushConverter : IValueConverter
{
    private static readonly IReadOnlyDictionary<WorkStatus, SolidColorBrush> Brushes = new Dictionary<WorkStatus, SolidColorBrush>
    {
        [WorkStatus.Todo] = Palette.Brush(Color.FromRgb(0x64, 0x74, 0x8B)),
        [WorkStatus.InProgress] = Palette.Brush(Color.FromRgb(0x3B, 0x82, 0xF6)),
        [WorkStatus.Review] = Palette.Brush(Color.FromRgb(0xA8, 0x55, 0xF7)),
        [WorkStatus.Done] = Palette.Brush(Color.FromRgb(0x22, 0xC5, 0x5E)),
        [WorkStatus.Blocked] = Palette.Brush(Color.FromRgb(0xEF, 0x44, 0x44)),
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var brush = value is WorkStatus s && Brushes.TryGetValue(s, out var b) ? b : Brushes[WorkStatus.Todo];

        // "Fill" gives the translucent pill background, matching the category pill idiom.
        if (parameter as string == "Fill")
        {
            return Palette.Brush(brush.Color, 0x2E);
        }

        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
