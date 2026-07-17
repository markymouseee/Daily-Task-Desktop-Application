using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleAssignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskAssignees",
                columns: table => new
                {
                    AssigneesId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskItemId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignees", x => new { x.AssigneesId, x.TaskItemId });
                    table.ForeignKey(
                        name: "FK_TaskAssignees_Tasks_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskAssignees_TeamMembers_AssigneesId",
                        column: x => x.AssigneesId,
                        principalTable: "TeamMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignees_TaskItemId",
                table: "TaskAssignees",
                column: "TaskItemId");

            // Carry existing single assignees over as the first multi-assignee.
            migrationBuilder.Sql(
                "INSERT INTO TaskAssignees (AssigneesId, TaskItemId) " +
                "SELECT AssignedToId, Id FROM Tasks WHERE AssignedToId IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaskAssignees");
        }
    }
}
