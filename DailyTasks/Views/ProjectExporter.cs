using System.IO;
using DailyTasks.Models;
using DailyTasks.Services;
using Microsoft.Win32;

namespace DailyTasks.Views;

public sealed class ProjectExporter : IProjectExporter
{
    public string? PromptForPath(Project project)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export SDLC to Excel",
            FileName = $"{SafeFileName(project.TaskItem.Title)}_SDLC_{DateTime.Now:yyyyMMdd}.xlsx",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public Task WriteAsync(Project project, string path) =>
        Task.Run(() => ProjectWorkbook.Save(project, path));

    private static string SafeFileName(string title)
    {
        var cleaned = new string(title.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
        return cleaned.Length == 0 ? "Project" : cleaned;
    }
}
