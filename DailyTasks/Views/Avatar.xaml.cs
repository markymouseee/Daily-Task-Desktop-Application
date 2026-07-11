using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DailyTasks.Views;

/// <summary>
/// A circular initials badge — the shared avatar style across the app. No photos, so it
/// stays fully local. Two-letter initials on a coloured background.
/// </summary>
public partial class Avatar : UserControl
{
    public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
        nameof(DisplayName), typeof(string), typeof(Avatar),
        new PropertyMetadata(string.Empty, OnVisualChanged));

    public static readonly DependencyProperty ColorHexProperty = DependencyProperty.Register(
        nameof(ColorHex), typeof(string), typeof(Avatar),
        new PropertyMetadata("#64748B", OnVisualChanged));

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(Avatar),
        new PropertyMetadata(28.0, OnVisualChanged));

    public Avatar()
    {
        InitializeComponent();
        Apply();
    }

    public string DisplayName
    {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public string ColorHex
    {
        get => (string)GetValue(ColorHexProperty);
        set => SetValue(ColorHexProperty, value);
    }

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public static string Initials(string? name)
    {
        var parts = (name ?? string.Empty).Split([' ', '.', '-', '_'], StringSplitOptions.RemoveEmptyEntries);

        var initials = parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1],
            _ => $"{parts[0][0]}{parts[^1][0]}",
        };

        return initials.ToUpperInvariant();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Avatar)d).Apply();

    private void Apply()
    {
        InitialsText.Text = Initials(DisplayName);
        InitialsText.FontSize = Math.Max(9, Size * 0.42);

        try
        {
            Root.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorHex));
        }
        catch (FormatException)
        {
            Root.Background = Brushes.SlateGray;
        }
    }
}
