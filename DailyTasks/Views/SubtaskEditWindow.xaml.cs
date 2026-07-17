using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyTasks.Models;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class SubtaskEditWindow : FluentWindow
{
    private static readonly TaskPriority[] PriorityByIndex =
        [TaskPriority.High, TaskPriority.Medium, TaskPriority.Low];

    private readonly TaskItem _subtask;
    private readonly List<SelectableMember> _members;

    public SubtaskEditWindow(TaskItem subtask, bool developerFeatures, IReadOnlyList<TeamMember> team, bool showXpPractices = false)
    {
        _subtask = subtask;
        InitializeComponent();

        var isNew = subtask.Id == 0;
        Heading.Title = isNew ? "New subtask" : "Edit subtask";

        var assignedIds = subtask.Assignees.Select(a => a.Id).ToHashSet();
        _members = team
            .Select(m => new SelectableMember(m) { IsSelected = assignedIds.Contains(m.Id) })
            .ToList();
        AssigneeList.ItemsSource = _members;
        NoTeamHint.Visibility = team.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

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

        XpRow.Visibility = showXpPractices ? Visibility.Visible : Visibility.Collapsed;
        PairBox.IsChecked = subtask.XpPractices.HasFlag(XpPractice.PairProgramming);
        TddBox.IsChecked = subtask.XpPractices.HasFlag(XpPractice.TestDriven);
        ReviewBox.IsChecked = subtask.XpPractices.HasFlag(XpPractice.CodeReview);

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
        _subtask.Assignees.Clear();
        foreach (var selected in _members.Where(sm => sm.IsSelected))
        {
            _subtask.Assignees.Add(selected.Member);
        }

        // Keep the single "primary" assignee in sync for the simpler single-avatar views.
        var primary = _subtask.Assignees.FirstOrDefault();
        _subtask.AssignedToId = primary?.Id;
        _subtask.AssignedTo = primary;
        _subtask.WhyReason = Blank(WhyBox.Text);
        _subtask.ContextResumeNote = Blank(ResumeBox.Text);

        _subtask.BlockedReason = _subtask.Status == WorkStatus.Blocked ? Blank(BlockedBox.Text) : null;

        if (XpRow.Visibility == Visibility.Visible)
        {
            var practices = XpPractice.None;
            if (PairBox.IsChecked == true) practices |= XpPractice.PairProgramming;
            if (TddBox.IsChecked == true) practices |= XpPractice.TestDriven;
            if (ReviewBox.IsChecked == true) practices |= XpPractice.CodeReview;
            _subtask.XpPractices = practices;
        }

        if (GitRow.Visibility == Visibility.Visible)
        {
            _subtask.GitLink = Blank(GitBox.Text);
        }

        DialogResult = true;
    }

    private static string? Blank(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    /// <summary>A team member with a checkbox state, for the multi-assignee list.</summary>
    private sealed class SelectableMember(TeamMember member)
    {
        public TeamMember Member { get; } = member;

        public bool IsSelected { get; set; }

        public Brush ColorBrush { get; } = ToBrush(member.InitialsColorHex);

        private static Brush ToBrush(string hex)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return Brushes.Gray;
            }
        }
    }
}
