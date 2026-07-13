using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPerProjectTeamAndRepo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerProjectId",
                table: "TeamMembers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitRepoPath",
                table: "Tasks",
                type: "TEXT",
                maxLength: 400,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_OwnerProjectId",
                table: "TeamMembers",
                column: "OwnerProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamMembers_Tasks_OwnerProjectId",
                table: "TeamMembers",
                column: "OwnerProjectId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamMembers_Tasks_OwnerProjectId",
                table: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_TeamMembers_OwnerProjectId",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "OwnerProjectId",
                table: "TeamMembers");

            migrationBuilder.DropColumn(
                name: "GitRepoPath",
                table: "Tasks");
        }
    }
}
