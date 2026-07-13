using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExpandMethodologies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remap legacy methodology names that no longer exist as enum members, so
            // existing rows still parse. "Iterative" became "IterativeIncremental";
            // "Custom" was removed, so those tasks fall back to unstructured.
            migrationBuilder.Sql("UPDATE Tasks SET Methodology = 'IterativeIncremental' WHERE Methodology = 'Iterative';");
            migrationBuilder.Sql("UPDATE Tasks SET Methodology = NULL WHERE Methodology = 'Custom';");

            migrationBuilder.DropColumn(
                name: "CustomPhases",
                table: "Tasks");

            migrationBuilder.AddColumn<string>(
                name: "ScrumRole",
                table: "TeamMembers",
                type: "TEXT",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SprintLengthDays",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WipLimit",
                table: "Tasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "XpPractices",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PairedPhaseId",
                table: "Phases",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Phases_PairedPhaseId",
                table: "Phases",
                column: "PairedPhaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Phases_Phases_PairedPhaseId",
                table: "Phases",
                column: "PairedPhaseId",
                principalTable: "Phases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Phases_Phases_PairedPhaseId",
                table: "Phases");

            migrationBuilder.DropIndex(
                name: "IX_Phases_PairedPhaseId",
                table: "Phases");

            migrationBuilder.DropColumn(
                name: "ScrumRole",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "SprintLengthDays",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "WipLimit",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "XpPractices",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PairedPhaseId",
                table: "Phases");

            migrationBuilder.AddColumn<string>(
                name: "CustomPhases",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
