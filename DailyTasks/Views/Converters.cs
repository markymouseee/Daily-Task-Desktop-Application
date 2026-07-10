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
            [TaskPriority.Low] = Color.FromRgb(0x64, 0x74, 0x8B),
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

public sealed class PriorityToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is TaskPriority priority && Palette.Priority.TryGetValue(priority, out var color)
            ? Palette.Brush(color)
            : Brushes.Transparent;

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
