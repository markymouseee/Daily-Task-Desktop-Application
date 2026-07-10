using System.Windows;
using DailyTasks.Models;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class EditTaskWindow : FluentWindow
{
    private readonly TaskItem _task;

    public EditTaskWindow(TaskItem task)
    {
        _task = task;
        InitializeComponent();

        TitleBox.Text = task.Title;
        WhyBox.Text = task.WhyReason ?? string.Empty;
        EstimateBox.Value = task.EstimatedMinutes;
        ResumeBox.Text = task.ContextResumeNote ?? string.Empty;
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
        _task.WhyReason = string.IsNullOrWhiteSpace(WhyBox.Text) ? null : WhyBox.Text.Trim();
        _task.EstimatedMinutes = EstimateBox.Value is > 0 and var v ? (int)v : null;
        _task.ContextResumeNote = string.IsNullOrWhiteSpace(ResumeBox.Text) ? null : ResumeBox.Text.Trim();

        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
