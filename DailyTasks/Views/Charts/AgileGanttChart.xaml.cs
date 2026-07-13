using System.Windows.Controls;
using System.Windows.Input;
using DailyTasks.Models;
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

    /// <summary>Commit an inline "% done" edit once the dropdown closes (a no-op if unchanged).</summary>
    private void OnPercentDropDownClosed(object? sender, EventArgs e)
    {
        if (sender is ComboBox { DataContext: AgileGanttRow row, SelectedValue: WorkStatus status }
            && DataContext is AgileGanttViewModel gantt)
        {
            gantt.SetStatus(row, status);
        }
    }
}
