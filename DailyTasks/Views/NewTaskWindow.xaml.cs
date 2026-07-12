using System.Windows;
using DailyTasks.Models;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class NewTaskWindow : FluentWindow
{
    private static readonly TaskPriority[] PriorityByIndex =
        [TaskPriority.High, TaskPriority.Medium, TaskPriority.Low];

    public NewTaskWindow(IReadOnlyList<Category> categories, Category? preferred)
    {
        InitializeComponent();

        CategoryBox.ItemsSource = categories;
        CategoryBox.SelectedItem = preferred ?? (categories.Count > 0 ? categories[0] : null);
        DueBox.SelectedDate = DateTime.Today;
    }

    public string TaskTitle { get; private set; } = string.Empty;

    public Category SelectedCategory { get; private set; } = null!;

    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; private set; }

    private void OnCreate(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();

        if (title.Length == 0 || CategoryBox.SelectedItem is not Category category)
        {
            TitleBox.Focus();
            return;
        }

        TaskTitle = title;
        SelectedCategory = category;
        Priority = PriorityByIndex[Math.Clamp(PriorityBox.SelectedIndex, 0, 2)];
        DueDate = DueBox.SelectedDate;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
