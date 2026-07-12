using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifyTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite enforces FKs during the table rebuilds this migration performs; turn it
            // off around the whole thing (outside the transaction) so the data transform can
            // reshape rows without tripping transient constraint checks.
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;", suppressTransaction: true);

            // ---- 1) Remove the Phases->Projects FK but keep the column/data for now ----
            migrationBuilder.DropForeignKey(
                name: "FK_Phases_Projects_ProjectId",
                table: "Phases");

            // ---- 2) Add the unified columns to Tasks (new columns, not renames) ----
            migrationBuilder.AddColumn<int>(name: "ParentTaskId", table: "Tasks", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "PhaseId", table: "Tasks", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "StartDate", table: "Tasks", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<double>(name: "EstimatedHours", table: "Tasks", type: "REAL", nullable: true);
            migrationBuilder.AddColumn<double>(name: "ActualHours", table: "Tasks", type: "REAL", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Status", table: "Tasks", type: "TEXT", maxLength: 16, nullable: false, defaultValue: "Todo");
            migrationBuilder.AddColumn<string>(name: "BlockedReason", table: "Tasks", type: "TEXT", maxLength: 300, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Methodology", table: "Tasks", type: "TEXT", maxLength: 16, nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomPhases", table: "Tasks", type: "TEXT", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<int>(name: "IterationCount", table: "Tasks", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "IterationNumber", table: "Tasks", type: "INTEGER", nullable: true);
            migrationBuilder.AddColumn<int>(name: "AssignedToId", table: "Tasks", type: "INTEGER", nullable: true);

            // ---- 3) Point Phases at their owning task (rename keeps the old project-id values) ----
            migrationBuilder.RenameColumn(name: "ProjectId", table: "Phases", newName: "OwnerTaskId");
            migrationBuilder.RenameIndex(name: "IX_Phases_ProjectId", table: "Phases", newName: "IX_Phases_OwnerTaskId");

            // ---- 4) Migrate the data (no-ops on a fresh DB where these tables are empty) ----
            migrationBuilder.Sql("UPDATE Tasks SET Status = CASE WHEN IsCompleted = 1 THEN 'Done' ELSE 'Todo' END;");
            migrationBuilder.Sql("UPDATE Tasks SET EstimatedHours = ROUND(EstimatedMinutes / 60.0, 4) WHERE EstimatedMinutes IS NOT NULL;");
            migrationBuilder.Sql("UPDATE Tasks SET ActualHours = ROUND(ActualMinutes / 60.0, 4) WHERE ActualMinutes IS NOT NULL;");

            // Methodology moves from the Project row onto its heading task.
            migrationBuilder.Sql(@"
                UPDATE Tasks SET
                    Methodology = (SELECT p.Methodology FROM Projects p WHERE p.TaskItemId = Tasks.Id),
                    CustomPhases = COALESCE((SELECT p.CustomPhases FROM Projects p WHERE p.TaskItemId = Tasks.Id), ''),
                    IterationCount = (SELECT p.IterationCount FROM Projects p WHERE p.TaskItemId = Tasks.Id)
                WHERE Id IN (SELECT TaskItemId FROM Projects);");

            // Phases: OwnerTaskId currently holds the old project id — remap to the heading task.
            migrationBuilder.Sql("UPDATE Phases SET OwnerTaskId = (SELECT p.TaskItemId FROM Projects p WHERE p.Id = Phases.OwnerTaskId);");

            // Subtasks become child tasks under their project's heading task.
            migrationBuilder.Sql(@"
                INSERT INTO Tasks (Title, CategoryId, Priority, ParentTaskId, StartDate, DueDate,
                                   EstimatedHours, ActualHours, Status, IsCompleted, BlockedReason, WhyReason,
                                   ContextResumeNote, GitLink, PhaseId, IterationNumber, AssignedToId,
                                   Recurrence, PostponedCount, CustomPhases, TaskType, CreatedAt)
                SELECT s.Title, head.CategoryId, s.Priority, p.TaskItemId, s.StartDate, s.DueDate,
                       s.EstimatedHours, s.ActualHours, s.Status,
                       CASE WHEN s.Status = 'Done' THEN 1 ELSE 0 END, s.BlockedReason, s.WhyReason,
                       s.ContextResumeNote, s.GitLinkPattern, s.PhaseId, s.IterationNumber, s.AssignedToId,
                       'None', 0, '', 'Simple', s.CreatedAt
                FROM Subtasks s
                JOIN Projects p ON s.ProjectId = p.Id
                JOIN Tasks head ON head.Id = p.TaskItemId;");

            // ---- 5) Drop the old tables and legacy columns ----
            migrationBuilder.DropTable(name: "Subtasks");
            migrationBuilder.DropTable(name: "Projects");

            migrationBuilder.DropIndex(name: "IX_Tasks_IsCompleted", table: "Tasks");
            migrationBuilder.DropColumn(name: "IsCompleted", table: "Tasks");
            migrationBuilder.DropColumn(name: "TaskType", table: "Tasks");
            migrationBuilder.DropColumn(name: "EstimatedMinutes", table: "Tasks");
            migrationBuilder.DropColumn(name: "ActualMinutes", table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedToId",
                table: "Tasks",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentTaskId",
                table: "Tasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_PhaseId",
                table: "Tasks",
                column: "PhaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Phases_Tasks_OwnerTaskId",
                table: "Phases",
                column: "OwnerTaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Phases_PhaseId",
                table: "Tasks",
                column: "PhaseId",
                principalTable: "Phases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Tasks_ParentTaskId",
                table: "Tasks",
                column: "ParentTaskId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_TeamMembers_AssignedToId",
                table: "Tasks",
                column: "AssignedToId",
                principalTable: "TeamMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.Sql("PRAGMA foreign_keys = ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Phases_Tasks_OwnerTaskId",
                table: "Phases");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Phases_PhaseId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Tasks_ParentTaskId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_TeamMembers_AssignedToId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AssignedToId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ParentTaskId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_PhaseId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ActualHours",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "BlockedReason",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "CustomPhases",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EstimatedHours",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IterationCount",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IterationNumber",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Methodology",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "PhaseId",
                table: "Tasks",
                newName: "EstimatedMinutes");

            migrationBuilder.RenameColumn(
                name: "ParentTaskId",
                table: "Tasks",
                newName: "ActualMinutes");

            migrationBuilder.RenameColumn(
                name: "OwnerTaskId",
                table: "Phases",
                newName: "ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Phases_OwnerTaskId",
                table: "Phases",
                newName: "IX_Phases_ProjectId");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaskType",
                table: "Tasks",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "Simple");

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomPhases = table.Column<string>(type: "TEXT", nullable: false),
                    IterationCount = table.Column<int>(type: "INTEGER", nullable: true),
                    Methodology = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Tasks_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subtasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssignedToId = table.Column<int>(type: "INTEGER", nullable: true),
                    PhaseId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualHours = table.Column<double>(type: "REAL", nullable: true),
                    BlockedReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ContextResumeNote = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EstimatedHours = table.Column<double>(type: "REAL", nullable: true),
                    GitLinkPattern = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IterationNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    WhyReason = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subtasks_Phases_PhaseId",
                        column: x => x.PhaseId,
                        principalTable: "Phases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Subtasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subtasks_TeamMembers_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_IsCompleted",
                table: "Tasks",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TaskItemId",
                table: "Projects",
                column: "TaskItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_AssignedToId",
                table: "Subtasks",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_PhaseId",
                table: "Subtasks",
                column: "PhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_ProjectId",
                table: "Subtasks",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Phases_Projects_ProjectId",
                table: "Phases",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
