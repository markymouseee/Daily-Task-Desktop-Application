using System.Windows;
using DailyTasks.Services;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class RecapWindow : FluentWindow
{
    public RecapWindow(RecapStats stats)
    {
        InitializeComponent();

        CompletionLine.Text = stats.PlannedCount == 0
            ? $"{stats.CompletedCount} task{(stats.CompletedCount == 1 ? "" : "s")} completed today"
            : $"{stats.CompletedCount} of {stats.PlannedCount} planned tasks completed";

        if (stats.SlippedCategory is { } category && stats.SlippedOpenCount > 0)
        {
            SlippedLine.Text = $"{category} slipped most — {stats.SlippedOpenCount} still open";
        }
        else
        {
            SlippedRow.Visibility = Visibility.Collapsed;
        }

        if (stats.EstimatedHours > 0 || stats.ActualHours > 0)
        {
            TimeLine.Text = $"Estimated {stats.EstimatedHours:0.#}h · actually tracked {stats.ActualHours:0.#}h";
        }
        else
        {
            TimeRow.Visibility = Visibility.Collapsed;
        }

        if (stats.InterruptionCount > 0)
        {
            InterruptionLine.Text =
                $"{stats.InterruptionCount} interruption{(stats.InterruptionCount == 1 ? "" : "s")} · {stats.InterruptionMinutesLost} min lost";
        }
        else
        {
            InterruptionRow.Visibility = Visibility.Collapsed;
        }
    }

    private void OnDone(object sender, RoutedEventArgs e) => Close();
}
