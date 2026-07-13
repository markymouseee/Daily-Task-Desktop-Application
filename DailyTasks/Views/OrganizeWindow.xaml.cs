using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyTasks.Models;
using DailyTasks.Services;
using Wpf.Ui.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace DailyTasks.Views;

/// <summary>Picks a methodology for a task that has children (or removes its structure).</summary>
public partial class OrganizeWindow : FluentWindow
{
    /// <summary>Picker categories, in display order, each with its methodologies.</summary>
    private static readonly (string Heading, Methodology[] Items)[] Groups =
    [
        ("Sequential", [Methodology.Waterfall, Methodology.VModel]),
        ("Iterative / Cyclical", [Methodology.Spiral, Methodology.IterativeIncremental, Methodology.RAD]),
        ("Agile-based", [Methodology.Agile, Methodology.Scrum, Methodology.XP]),
        ("Continuous Flow", [Methodology.Kanban, Methodology.Lean, Methodology.DevOps]),
        ("Minimal", [Methodology.BigBang]),
    ];

    private readonly bool _askTitle;
    private readonly List<RadioButton> _radios = [];

    public OrganizeWindow(Methodology? current, bool askTitle = false)
    {
        InitializeComponent();

        _askTitle = askTitle;
        if (askTitle)
        {
            Title = "New project";
            HeaderBar.Title = "New project";
            TitleRow.Visibility = Visibility.Visible;
        }

        RemoveButton.Visibility = current is null || askTitle ? Visibility.Collapsed : Visibility.Visible;

        BuildOptions(current ?? Methodology.Waterfall);
        UpdateConditionalRows();
    }

    /// <summary>True = apply the chosen methodology; false with Remove = clear it; null = cancelled.</summary>
    public bool? Remove { get; private set; }

    /// <summary>The project name, when the dialog was opened in new-project mode.</summary>
    public string ProjectTitle { get; private set; } = string.Empty;

    public Methodology Methodology { get; private set; } = Methodology.Waterfall;

    /// <summary>Cycles (Spiral/Iterative/RAD) or sprints (Agile/Scrum/XP); null otherwise.</summary>
    public int? IterationCount { get; private set; }

    /// <summary>Sprint length in days for the agile methodologies; null otherwise.</summary>
    public int? SprintLengthDays { get; private set; }

    /// <summary>In-progress WIP limit for Lean; null otherwise.</summary>
    public int? WipLimit { get; private set; }

    private void BuildOptions(Methodology current)
    {
        foreach (var (heading, items) in Groups)
        {
            MethodologyList.Children.Add(new TextBlock
            {
                Text = heading.ToUpperInvariant(),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 12, 0, 6),
                Foreground = (Brush)FindResource("TextFillColorSecondaryBrush"),
            });

            foreach (var methodology in items)
            {
                var radio = new RadioButton
                {
                    GroupName = "M",
                    Margin = new Thickness(0, 0, 0, 6),
                    Tag = methodology,
                    IsChecked = methodology == current,
                    Content = OptionContent(methodology),
                };
                radio.Checked += OnMethodologyChanged;
                _radios.Add(radio);
                MethodologyList.Children.Add(radio);
            }
        }
    }

    private StackPanel OptionContent(Methodology methodology)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = TaskRules.DisplayName(methodology),
            FontWeight = FontWeights.SemiBold,
        });
        panel.Children.Add(new TextBlock
        {
            Text = TaskRules.Description(methodology),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)FindResource("TextFillColorTertiaryBrush"),
        });
        return panel;
    }

    private void OnMethodologyChanged(object sender, RoutedEventArgs e) => UpdateConditionalRows();

    private void UpdateConditionalRows()
    {
        if (!IsInitialized)
        {
            return;
        }

        var selected = Selected();
        var sprintBased = TaskRules.IsSprintBased(selected);
        var cyclical = TaskRules.UsesCycles(selected);

        IterationRow.Visibility = sprintBased || cyclical ? Visibility.Visible : Visibility.Collapsed;
        IterationLabel.Text = sprintBased ? "Number of sprints" : "Number of cycles";
        SprintLengthRow.Visibility = sprintBased ? Visibility.Visible : Visibility.Collapsed;
        WipRow.Visibility = selected == Methodology.Lean ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnApply(object sender, RoutedEventArgs e)
    {
        if (_askTitle)
        {
            ProjectTitle = TitleBox.Text.Trim();
            if (ProjectTitle.Length == 0)
            {
                TitleBox.Focus();
                return;
            }
        }

        Methodology = Selected();

        var wantsCount = TaskRules.IsSprintBased(Methodology) || TaskRules.UsesCycles(Methodology);
        IterationCount = wantsCount ? (int)IterationCountBox.Value.GetValueOrDefault(3) : null;
        SprintLengthDays = TaskRules.IsSprintBased(Methodology)
            ? (int)SprintLengthBox.Value.GetValueOrDefault(TaskRules.DefaultSprintLengthDays)
            : null;
        WipLimit = Methodology == Methodology.Lean && WipLimitBox.Value is { } wip ? (int)wip : null;

        Remove = false;
        DialogResult = true;
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        Remove = true;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private Methodology Selected() =>
        _radios.FirstOrDefault(r => r.IsChecked == true)?.Tag is Methodology m ? m : Methodology.Waterfall;
}
