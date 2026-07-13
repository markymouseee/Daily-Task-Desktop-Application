using System.IO;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<InterruptionEvent> Interruptions => Set<InterruptionEvent>();

    public DbSet<Phase> Phases => Set<Phase>();

    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

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
        task.Property(t => t.BlockedReason).HasMaxLength(300);

        // IsCompleted is a convenience over Status, not its own column.
        task.Ignore(t => t.IsCompleted);

        // Enums stored as text so the database stays readable and isn't silently
        // reinterpreted if enum members are ever reordered.
        task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);

        task.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(WorkStatus.Todo);

        // Nullable: null = a plain, unstructured task. Widened past 16 to fit the longer
        // member names (e.g. "IterativeIncremental").
        task.Property(t => t.Methodology).HasConversion<string>().HasMaxLength(24);

        task.Property(t => t.Recurrence)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(RecurrenceKind.None);

        // XP practice tags stored as their flags integer (0 = none).
        task.Property(t => t.XpPractices).HasDefaultValue(XpPractice.None);

        task.HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Self-referencing hierarchy; deleting a parent removes its whole subtree.
        task.HasOne(t => t.Parent)
            .WithMany(t => t.Children)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // A task outlives its phase (e.g. the phase is removed) by falling back to null.
        task.HasOne(t => t.Phase)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.PhaseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Removing a member unassigns their tasks rather than deleting the work.
        task.HasOne(t => t.AssignedTo)
            .WithMany(m => m.Tasks)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        task.HasIndex(t => t.DueDate);
        task.HasIndex(t => t.ParentTaskId);
        task.HasIndex(t => t.PhaseId);
        task.HasIndex(t => t.AssignedToId);

        var phase = modelBuilder.Entity<Phase>();

        phase.Property(p => p.Name).IsRequired().HasMaxLength(100);

        phase.HasOne(p => p.OwnerTask)
            .WithMany(t => t.Phases)
            .HasForeignKey(p => p.OwnerTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // V-Model pairing: a dev phase points at its test phase. Self-referencing, and
        // deleting one side simply clears the link rather than cascading.
        phase.HasOne(p => p.PairedPhase)
            .WithMany()
            .HasForeignKey(p => p.PairedPhaseId)
            .OnDelete(DeleteBehavior.SetNull);

        phase.HasIndex(p => p.OwnerTaskId);

        var member = modelBuilder.Entity<TeamMember>();

        member.Property(m => m.Name).IsRequired().HasMaxLength(100);
        member.Property(m => m.Role).HasMaxLength(60);
        member.Property(m => m.InitialsColorHex).IsRequired().HasMaxLength(9);
        member.Property(m => m.ScrumRole).HasConversion<string>().HasMaxLength(16);

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
