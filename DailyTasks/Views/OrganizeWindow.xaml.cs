using System.Windows;
using DailyTasks.Models;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

/// <summary>Picks a methodology for a task that has children (or removes its structure).</summary>
public partial class OrganizeWindow : FluentWindow
{
    private readonly bool _askTitle;

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

        switch (current)
        {
            case Methodology.Agile: AgileRadio.IsChecked = true; break;
            case Methodology.Iterative: IterativeRadio.IsChecked = true; break;
            case Methodology.Kanban: KanbanRadio.IsChecked = true; break;
            case Methodology.Custom: CustomRadio.IsChecked = true; break;
            default: WaterfallRadio.IsChecked = true; break;
        }

        UpdateConditionalRows();
    }

    /// <summary>True = apply the chosen methodology; false with Remove = clear it; null = cancelled.</summary>
    public bool? Remove { get; private set; }

    /// <summary>The project name, when the dialog was opened in new-project mode.</summary>
    public string ProjectTitle { get; private set; } = string.Empty;

    public Methodology Methodology { get; private set; } = Methodology.Waterfall;

    public IReadOnlyList<string> CustomPhases { get; private set; } = [];

    public int? IterationCount { get; private set; }

    private void OnMethodologyChanged(object sender, RoutedEventArgs e) => UpdateConditionalRows();

    private void UpdateConditionalRows()
    {
        if (!IsInitialized)
        {
            return;
        }

        IterationRow.Visibility = IterativeRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        CustomPhasesRow.Visibility = CustomRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
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
        CustomPhases = Methodology == Methodology.Custom ? ParsePhases() : [];
        IterationCount = Methodology == Methodology.Iterative ? (int)IterationCountBox.Value.GetValueOrDefault(3) : null;

        if (Methodology == Methodology.Custom && CustomPhases.Count == 0)
        {
            CustomPhasesBox.Focus();
            return;
        }

        Remove = false;
        DialogResult = true;
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        Remove = true;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private Methodology Selected()
    {
        if (AgileRadio.IsChecked == true) return Methodology.Agile;
        if (IterativeRadio.IsChecked == true) return Methodology.Iterative;
        if (KanbanRadio.IsChecked == true) return Methodology.Kanban;
        if (CustomRadio.IsChecked == true) return Methodology.Custom;
        return Methodology.Waterfall;
    }

    private IReadOnlyList<string> ParsePhases() =>
        CustomPhasesBox.Text
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}
