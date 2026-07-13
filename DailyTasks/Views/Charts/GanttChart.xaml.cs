using System.Windows.Controls;
using System.Windows.Input;
using DailyTasks.ViewModels;

namespace DailyTasks.Views;

/// <summary>
/// Custom Canvas Gantt chart. Keeps the frozen label/assignee columns and the week header in
/// sync with the scrolling timeline body; all date→pixel math lives in the view model.
/// </summary>
public partial class GanttChart : UserControl
{
    public GanttChart() => InitializeComponent();

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
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 || DataContext is not GanttViewModel gantt)
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
}
