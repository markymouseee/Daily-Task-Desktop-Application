using System.Windows;
using DailyTasks.Models;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

/// <summary>
/// The unified "create" dialog: a Simple task, or a Project with a methodology.
/// Exposes the user's choices as plain properties; the caller does the persisting.
/// </summary>
public partial class NewProjectWindow : FluentWindow
{
    private static readonly TaskPriority[] PriorityByIndex =
        [TaskPriority.High, TaskPriority.Medium, TaskPriority.Low];

    public NewProjectWindow(IReadOnlyList<Category> categories, Category? preferred)
    {
        InitializeComponent();

        CategoryBox.ItemsSource = categories;
        CategoryBox.SelectedItem = preferred ?? (categories.Count > 0 ? categories[0] : null);
        DueBox.SelectedDate = DateTime.Today;
    }

    /// <summary>True when the user chose Project rather than a Simple task.</summary>
    public bool IsProject { get; private set; }

    public Methodology Methodology { get; private set; } = Methodology.Waterfall;

    public IReadOnlyList<string> CustomPhases { get; private set; } = [];

    public int? IterationCount { get; private set; }

    public string ProjectTitle { get; private set; } = string.Empty;

    public Category SelectedCategory { get; private set; } = null!;

    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; private set; }

    private void OnTypeChanged(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        var project = ProjectToggle.IsChecked == true;
        MethodologySection.Visibility = project ? Visibility.Visible : Visibility.Collapsed;
        CreateButton.Content = project ? "Create project" : "Create task";
    }

    private void OnMethodologyChanged(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized)
        {
            return;
        }

        IterationRow.Visibility = IterativeRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        CustomPhasesRow.Visibility = CustomRadio.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnCreate(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();

        if (title.Length == 0 || CategoryBox.SelectedItem is not Category category)
        {
            TitleBox.Focus();
            return;
        }

        ProjectTitle = title;
        SelectedCategory = category;
        Priority = PriorityByIndex[Math.Clamp(PriorityBox.SelectedIndex, 0, 2)];
        DueDate = DueBox.SelectedDate;
        IsProject = ProjectToggle.IsChecked == true;

        if (IsProject)
        {
            Methodology = SelectedMethodology();
            CustomPhases = Methodology == Methodology.Custom ? ParseCustomPhases() : [];
            IterationCount = Methodology == Methodology.Iterative ? (int)IterationCountBox.Value.GetValueOrDefault(3) : null;

            // A custom project with no phases has nothing to plan against.
            if (Methodology == Methodology.Custom && CustomPhases.Count == 0)
            {
                CustomPhasesBox.Focus();
                return;
            }
        }

        DialogResult = true;
    }

    private Methodology SelectedMethodology()
    {
        if (AgileRadio.IsChecked == true) return Methodology.Agile;
        if (IterativeRadio.IsChecked == true) return Methodology.Iterative;
        if (KanbanRadio.IsChecked == true) return Methodology.Kanban;
        if (CustomRadio.IsChecked == true) return Methodology.Custom;
        return Methodology.Waterfall;
    }

    private IReadOnlyList<string> ParseCustomPhases() =>
        CustomPhasesBox.Text
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
