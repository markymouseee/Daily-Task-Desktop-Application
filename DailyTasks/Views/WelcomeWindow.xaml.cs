using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

/// <summary>
/// First-run walkthrough. A short, skippable tour of the app; the caller records that it ran so
/// it never shows again automatically (it can be reopened from Settings).
/// </summary>
public partial class WelcomeWindow : FluentWindow
{
    private sealed record Step(SymbolRegular Icon, string Title, string Body);

    private static readonly Step[] Steps =
    [
        new(SymbolRegular.Rocket24, "Welcome to DailyTasks",
            "Plan your day and run real projects — 100% on your PC, no account, no cloud. Here's a quick 30-second tour."),
        new(SymbolRegular.CalendarToday24, "Start with Today",
            "Type tasks in plain English like “pay rent friday !high” and the date and priority are filled in for you. Pin your Big 3 for the day, and press Ctrl + Shift + T anywhere in Windows to capture a task."),
        new(SymbolRegular.Flowchart24, "Turn work into projects",
            "Give a task subtasks, then organize it with a methodology — Agile, Scrum, Waterfall and more. Projects live on the Projects page with phases, a Gantt chart, a board, and Excel export."),
        new(SymbolRegular.DataBarHorizontal24, "See the big picture",
            "Track everything on the Calendar and the Gantt. In the Agile Gantt you can type a % done and assign several people to an activity — or the whole Team."),
        new(SymbolRegular.Code24, "For developers (optional)",
            "Turn on developer features in Settings to point a project at a git repo, watch its commits, and auto-complete tasks whose git link shows up in a commit message."),
        new(SymbolRegular.CheckmarkCircle24, "You're all set",
            "That's it! You can reopen this tour any time from Settings. Let's get started."),
    ];

    private int _index;

    public WelcomeWindow()
    {
        InitializeComponent();
        Show(0);
    }

    private void Show(int index)
    {
        _index = index;
        var step = Steps[index];

        StepIcon.Symbol = step.Icon;
        StepTitle.Text = step.Title;
        StepBody.Text = step.Body;

        var isFirst = index == 0;
        var isLast = index == Steps.Length - 1;

        BackButton.Visibility = isFirst ? Visibility.Collapsed : Visibility.Visible;
        SkipButton.Visibility = isLast ? Visibility.Collapsed : Visibility.Visible;
        NextButton.Content = isLast ? "Get started" : "Next";

        BuildDots();
    }

    private void BuildDots()
    {
        Dots.Items.Clear();
        for (var i = 0; i < Steps.Length; i++)
        {
            var active = i == _index;
            Dots.Items.Add(new Ellipse
            {
                Width = active ? 22 : 8,
                Height = 8,
                Margin = new Thickness(4, 0, 4, 0),
                Fill = active
                    ? (Brush)FindResource("AccentFillColorDefaultBrush")
                    : (Brush)FindResource("ControlFillColorSecondaryBrush"),
            });
        }
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        if (_index > 0)
        {
            Show(_index - 1);
        }
    }

    private void OnNext(object sender, RoutedEventArgs e)
    {
        if (_index < Steps.Length - 1)
        {
            Show(_index + 1);
        }
        else
        {
            DialogResult = true;
        }
    }

    private void OnSkip(object sender, RoutedEventArgs e) => DialogResult = true;
}
