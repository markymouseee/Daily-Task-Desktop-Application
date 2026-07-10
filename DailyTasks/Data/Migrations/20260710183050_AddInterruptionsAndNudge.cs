using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterruptionsAndNudge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastNudgedAt",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Interruptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskItemId = table.Column<int>(type: "INTEGER", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    MinutesLost = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interruptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interruptions_Tasks_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interruptions_OccurredAt",
                table: "Interruptions",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Interruptions_TaskItemId",
                table: "Interruptions",
                column: "TaskItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Interruptions");

            migrationBuilder.DropColumn(
                name: "LastNudgedAt",
                table: "Tasks");
        }
    }
}
