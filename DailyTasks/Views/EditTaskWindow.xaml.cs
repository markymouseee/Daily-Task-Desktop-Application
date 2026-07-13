using System.Windows;
using DailyTasks.Models;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class EditTaskWindow : FluentWindow
{
    private static readonly TaskPriority[] PriorityByIndex =
        [TaskPriority.High, TaskPriority.Medium, TaskPriority.Low];

    private readonly TaskItem _task;

    public EditTaskWindow(TaskItem task, bool developerFeatures)
    {
        _task = task;
        InitializeComponent();

        TitleBox.Text = task.Title;
        PriorityBox.SelectedIndex = Array.IndexOf(PriorityByIndex, task.Priority) is var p and >= 0 ? p : 1;
        WhyBox.Text = task.WhyReason ?? string.Empty;
        EstimateBox.Value = task.EstimatedHours;
        ResumeBox.Text = task.ContextResumeNote ?? string.Empty;
        GitBox.Text = task.GitLink ?? string.Empty;
        RecurrenceBox.SelectedIndex = (int)task.Recurrence;

        GitRow.Visibility = developerFeatures ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();

        if (title.Length == 0)
        {
            TitleBox.Focus();
            return;
        }

        _task.Title = title;
        _task.Priority = PriorityByIndex[Math.Clamp(PriorityBox.SelectedIndex, 0, 2)];
        _task.WhyReason = string.IsNullOrWhiteSpace(WhyBox.Text) ? null : WhyBox.Text.Trim();
        _task.EstimatedHours = EstimateBox.Value is > 0 and var v ? v : null;
        _task.ContextResumeNote = string.IsNullOrWhiteSpace(ResumeBox.Text) ? null : ResumeBox.Text.Trim();
        _task.Recurrence = (RecurrenceKind)RecurrenceBox.SelectedIndex;

        if (GitRow.Visibility == Visibility.Visible)
        {
            _task.GitLink = string.IsNullOrWhiteSpace(GitBox.Text) ? null : GitBox.Text.Trim();
        }

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
