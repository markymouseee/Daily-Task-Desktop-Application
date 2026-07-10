namespace DailyTasks.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Pill colour, as "#RRGGBB".</summary>
    public string ColorHex { get; set; } = "#64748B";

    public ICollection<TaskItem> Tasks { get; } = [];
}
