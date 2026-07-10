using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace DailyTasks.Views;

public partial class ResumeNoteWindow : FluentWindow
{
    public ResumeNoteWindow(string? existingNote)
    {
        InitializeComponent();

        NoteBox.Text = existingNote ?? string.Empty;

        Loaded += (_, _) => NoteBox.Focus();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    public string Note => NoteBox.Text;

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                DialogResult = true;
                break;
            case Key.Escape:
                e.Handled = true;
                DialogResult = false;
                break;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnSkip(object sender, RoutedEventArgs e) => DialogResult = false;
}
