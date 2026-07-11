using System.IO;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<InterruptionEvent> Interruptions => Set<InterruptionEvent>();

    /// <summary>
    /// %LOCALAPPDATA%\DailyTasks\dailytasks.db
    /// </summary>
    public static string DatabasePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DailyTasks",
        "dailytasks.db");

    public static string ConnectionString => $"Data Source={DatabasePath}";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var category = modelBuilder.Entity<Category>();
        category.Property(c => c.Name).IsRequired().HasMaxLength(50);
        category.Property(c => c.ColorHex).IsRequired().HasMaxLength(9);
        category.HasIndex(c => c.Name).IsUnique();

        category.HasData(
            new { Id = 1, Name = "Work", ColorHex = "#3B82F6" },
            new { Id = 2, Name = "Personal", ColorHex = "#A855F7" },
            new { Id = 3, Name = "Errands", ColorHex = "#10B981" });

        var task = modelBuilder.Entity<TaskItem>();

        task.Property(t => t.Title).IsRequired().HasMaxLength(200);
        task.Property(t => t.Notes).HasMaxLength(2000);
        task.Property(t => t.WhyReason).HasMaxLength(300);
        task.Property(t => t.ContextResumeNote).HasMaxLength(500);
        task.Property(t => t.GitLink).HasMaxLength(200);

        // Store the enum as text so the database stays readable and is not
        // silently reinterpreted if enum members are ever reordered.
        task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);

        task.HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        task.HasIndex(t => t.IsCompleted);
        task.HasIndex(t => t.DueDate);

        var interruption = modelBuilder.Entity<InterruptionEvent>();

        interruption.Property(i => i.Reason).HasConversion<string>().HasMaxLength(16);

        // Interruption history stays useful for Insights even after its task is gone.
        interruption.HasOne(i => i.TaskItem)
            .WithMany()
            .HasForeignKey(i => i.TaskItemId)
            .OnDelete(DeleteBehavior.SetNull);

        interruption.HasIndex(i => i.OccurredAt);
    }
}
