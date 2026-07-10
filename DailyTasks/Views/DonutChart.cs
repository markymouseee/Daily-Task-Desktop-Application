using System.Collections;
using System.Windows;
using System.Windows.Media;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

/// <summary>
/// A minimal donut chart for the category balance card. Renders
/// <see cref="CategorySlice"/> items; colours come from each slice's hex.
/// </summary>
public sealed class DonutChart : FrameworkElement
{
    public static readonly DependencyProperty SlicesProperty = DependencyProperty.Register(
        nameof(Slices),
        typeof(IEnumerable),
        typeof(DonutChart),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSlicesChanged));

    public IEnumerable? Slices
    {
        get => (IEnumerable?)GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    private static void OnSlicesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // ObservableCollection is rebuilt via Clear+Add, so also redraw on collection changes.
        var chart = (DonutChart)d;

        if (e.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldCol)
        {
            oldCol.CollectionChanged -= chart.OnCollectionChanged;
        }

        if (e.NewValue is System.Collections.Specialized.INotifyCollectionChanged newCol)
        {
            newCol.CollectionChanged += chart.OnCollectionChanged;
        }
    }

    private void OnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();

    protected override void OnRender(DrawingContext dc)
    {
        var slices = Slices?.Cast<CategorySlice>().Where(s => s.Count > 0).ToList();

        if (slices is null || slices.Count == 0)
        {
            return;
        }

        var total = slices.Sum(s => s.Count);
        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var outer = Math.Min(ActualWidth, ActualHeight) / 2;
        var inner = outer * 0.62;

        if (outer <= 0 || total <= 0)
        {
            return;
        }

        // A single slice is a full ring; arcs degenerate at 360°, so draw it directly.
        if (slices.Count == 1)
        {
            var ring = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new EllipseGeometry(center, outer, outer),
                new EllipseGeometry(center, inner, inner));
            dc.DrawGeometry(BrushFor(slices[0]), null, ring);
            return;
        }

        var angle = -90.0;

        foreach (var slice in slices)
        {
            var sweep = 360.0 * slice.Count / total;
            dc.DrawGeometry(BrushFor(slice), null, BuildSegment(center, outer, inner, angle, sweep));
            angle += sweep;
        }
    }

    private static Geometry BuildSegment(Point center, double outer, double inner, double startDeg, double sweepDeg)
    {
        // A hair's gap keeps anti-aliased slice edges from bleeding into each other.
        var start = startDeg + 0.6;
        var end = startDeg + sweepDeg - 0.6;

        if (end <= start)
        {
            end = startDeg + sweepDeg;
            start = startDeg;
        }

        var large = end - start > 180;

        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(PointAt(center, outer, start), isFilled: true, isClosed: true);
            ctx.ArcTo(PointAt(center, outer, end), new Size(outer, outer), 0, large, SweepDirection.Clockwise, true, false);
            ctx.LineTo(PointAt(center, inner, end), true, false);
            ctx.ArcTo(PointAt(center, inner, start), new Size(inner, inner), 0, large, SweepDirection.Counterclockwise, true, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static Point PointAt(Point center, double radius, double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        return new Point(center.X + (radius * Math.Cos(rad)), center.Y + (radius * Math.Sin(rad)));
    }

    private static SolidColorBrush BrushFor(CategorySlice slice)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(slice.ColorHex);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
        catch (FormatException)
        {
            return Brushes.Gray;
        }
    }
}
