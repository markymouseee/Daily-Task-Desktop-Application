using System.Windows.Controls;
using System.Windows.Input;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

/// <summary>
/// Row-based Agile Gantt: a frozen Sprint/Activity/Assigned/Start/End/Duration/Status/% table
/// kept in sync with a scrolling, %-filled calendar timeline. Date→pixel math lives in the
/// view model; this only wires the synced scrolling and Ctrl-wheel zoom.
/// </summary>
public partial class AgileGanttChart : UserControl
{
    public AgileGanttChart() => InitializeComponent();

    private void OnBodyScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.HorizontalChange != 0)
        {
            HeaderScroll.ScrollToHorizontalOffset(e.HorizontalOffset);
        }

        if (e.VerticalChange != 0)
        {
            LabelScroll.ScrollToVerticalOffset(e.VerticalOffset);
        }
    }

    private void OnBodyWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 || DataContext is not AgileGanttViewModel gantt)
        {
            return;
        }

        if (e.Delta > 0)
        {
            gantt.ZoomInCommand.Execute(null);
        }
        else
        {
            gantt.ZoomOutCommand.Execute(null);
        }

        e.Handled = true;
    }

    /// <summary>Select the whole value on focus so typing replaces it.</summary>
    private void OnPercentFocused(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox box)
        {
            box.Dispatcher.BeginInvoke(box.SelectAll);
        }
    }

    /// <summary>Commit a typed "% done" on Enter.</summary>
    private void OnPercentKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox box)
        {
            CommitPercent(box);
            e.Handled = true;
        }
    }

    /// <summary>Commit a typed "% done" when the field loses focus (a no-op if unchanged).</summary>
    private void OnPercentCommit(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is TextBox box)
        {
            CommitPercent(box);
        }
    }

    private void CommitPercent(TextBox box)
    {
        if (box.DataContext is not AgileGanttRow row || DataContext is not AgileGanttViewModel gantt)
        {
            return;
        }

        // Accept "30", "30%", or any text with digits.
        var digits = new string((box.Text ?? string.Empty).Where(char.IsDigit).ToArray());
        if (int.TryParse(digits, out var percent))
        {
            gantt.SetPercent(row, percent);
        }
    }
}
