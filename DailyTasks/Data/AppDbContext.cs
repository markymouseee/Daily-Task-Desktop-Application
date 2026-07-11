using System.IO;
using System.Text.Json;
using DailyTasks.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DailyTasks.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<InterruptionEvent> Interruptions => Set<InterruptionEvent>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Phase> Phases => Set<Phase>();

    public DbSet<Subtask> Subtasks => Set<Subtask>();

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

        // Store enums as text so the database stays readable and is not
        // silently reinterpreted if enum members are ever reordered.
        task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);

        // Default to "Simple" so rows that predate this column upgrade cleanly.
        task.Property(t => t.TaskType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(TaskType.Simple);

        // Default to "None" so rows that predate this column parse back cleanly on upgrade.
        task.Property(t => t.Recurrence)
            .HasConversion<string>()
            .HasMaxLength(16)
            .HasDefaultValue(RecurrenceKind.None);

        task.HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        task.HasIndex(t => t.IsCompleted);
        task.HasIndex(t => t.DueDate);

        // ---- Projects: phases and subtasks ----

        var project = modelBuilder.Entity<Project>();

        project.Property(p => p.Methodology).HasConversion<string>().HasMaxLength(16);

        // Custom phase names ride along as a small JSON array; comparing the list
        // by value keeps EF's change tracking honest for a mutable reference type.
        var stringListComparer = new ValueComparer<List<string>>(
            (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList());

        project.Property(p => p.CustomPhases)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .Metadata.SetValueComparer(stringListComparer);

        // One project per task; deleting the task tears down its whole plan.
        project.HasOne(p => p.TaskItem)
            .WithOne(t => t.Project!)
            .HasForeignKey<Project>(p => p.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        project.HasIndex(p => p.TaskItemId).IsUnique();

        var phase = modelBuilder.Entity<Phase>();

        phase.Property(p => p.Name).IsRequired().HasMaxLength(100);

        phase.HasOne(p => p.Project)
            .WithMany(p => p.Phases)
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        phase.HasIndex(p => p.ProjectId);

        var subtask = modelBuilder.Entity<Subtask>();

        subtask.Property(s => s.Title).IsRequired().HasMaxLength(200);
        subtask.Property(s => s.Priority).HasConversion<string>().HasMaxLength(16);
        subtask.Property(s => s.Status).HasConversion<string>().HasMaxLength(16);
        subtask.Property(s => s.BlockedReason).HasMaxLength(300);
        subtask.Property(s => s.WhyReason).HasMaxLength(300);
        subtask.Property(s => s.ContextResumeNote).HasMaxLength(500);
        subtask.Property(s => s.GitLinkPattern).HasMaxLength(200);

        subtask.HasOne(s => s.Project)
            .WithMany(p => p.Subtasks)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // A subtask outlives its phase (e.g. a phase is removed) by falling back
        // to a null phase rather than vanishing with it.
        subtask.HasOne(s => s.Phase)
            .WithMany(p => p.Subtasks)
            .HasForeignKey(s => s.PhaseId)
            .OnDelete(DeleteBehavior.SetNull);

        subtask.HasIndex(s => s.ProjectId);
        subtask.HasIndex(s => s.PhaseId);

        var member = modelBuilder.Entity<TeamMember>();

        member.Property(m => m.Name).IsRequired().HasMaxLength(100);
        member.Property(m => m.Role).HasMaxLength(60);
        member.Property(m => m.InitialsColorHex).IsRequired().HasMaxLength(9);

        // Unassign a member's subtasks rather than deleting them when the member is removed.
        subtask.HasOne(s => s.AssignedTo)
            .WithMany(m => m.Subtasks)
            .HasForeignKey(s => s.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        subtask.HasIndex(s => s.AssignedToId);

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
