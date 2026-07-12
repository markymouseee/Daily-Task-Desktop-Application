using System.Windows;
using System.Windows.Controls;
using DailyTasks.Models;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class SubtaskEditWindow : FluentWindow
{
    private static readonly TaskPriority[] PriorityByIndex =
        [TaskPriority.High, TaskPriority.Medium, TaskPriority.Low];

    private static readonly TeamMember Unassigned = new() { Id = 0, Name = "Unassigned" };

    private readonly TaskItem _subtask;

    public SubtaskEditWindow(TaskItem subtask, bool developerFeatures, IReadOnlyList<TeamMember> team)
    {
        _subtask = subtask;
        InitializeComponent();

        var isNew = subtask.Id == 0;
        Heading.Title = isNew ? "New subtask" : "Edit subtask";

        var options = new List<TeamMember> { Unassigned };
        options.AddRange(team);
        AssigneeBox.ItemsSource = options;
        AssigneeBox.SelectedItem = options.FirstOrDefault(m => m.Id == subtask.AssignedToId) ?? Unassigned;

        TitleBox.Text = subtask.Title;
        PriorityBox.SelectedIndex = Array.IndexOf(PriorityByIndex, subtask.Priority) is var p and >= 0 ? p : 1;
        StatusBox.SelectedIndex = (int)subtask.Status;
        EstimateBox.Value = subtask.EstimatedHours;
        ActualBox.Value = subtask.ActualHours;
        StartBox.SelectedDate = subtask.StartDate;
        DueBox.SelectedDate = subtask.DueDate;
        BlockedBox.Text = subtask.BlockedReason ?? string.Empty;
        WhyBox.Text = subtask.WhyReason ?? string.Empty;
        ResumeBox.Text = subtask.ContextResumeNote ?? string.Empty;
        GitBox.Text = subtask.GitLink ?? string.Empty;

        GitRow.Visibility = developerFeatures ? Visibility.Visible : Visibility.Collapsed;
        UpdateBlockedVisibility();
    }

    private void OnStatusChanged(object sender, SelectionChangedEventArgs e) => UpdateBlockedVisibility();

    private void UpdateBlockedVisibility()
    {
        if (!IsInitialized)
        {
            return;
        }

        BlockedRow.Visibility = StatusBox.SelectedIndex == (int)WorkStatus.Blocked
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();

        if (title.Length == 0)
        {
            TitleBox.Focus();
            return;
        }

        _subtask.Title = title;
        _subtask.Priority = PriorityByIndex[Math.Clamp(PriorityBox.SelectedIndex, 0, 2)];
        _subtask.Status = (WorkStatus)StatusBox.SelectedIndex;
        _subtask.EstimatedHours = EstimateBox.Value is > 0 and var est ? est : null;
        _subtask.ActualHours = ActualBox.Value is > 0 and var act ? act : null;
        _subtask.StartDate = StartBox.SelectedDate;
        _subtask.DueDate = DueBox.SelectedDate;
        var assignee = AssigneeBox.SelectedItem as TeamMember is { Id: > 0 } m ? m : null;
        _subtask.AssignedToId = assignee?.Id;
        _subtask.AssignedTo = assignee;
        _subtask.WhyReason = Blank(WhyBox.Text);
        _subtask.ContextResumeNote = Blank(ResumeBox.Text);

        _subtask.BlockedReason = _subtask.Status == WorkStatus.Blocked ? Blank(BlockedBox.Text) : null;

        if (GitRow.Visibility == Visibility.Visible)
        {
            _subtask.GitLink = Blank(GitBox.Text);
        }

        DialogResult = true;
    }

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
