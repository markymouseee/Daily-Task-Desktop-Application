using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "Subtasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    InitialsColorHex = table.Column<string>(type: "TEXT", maxLength: 9, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_AssignedToId",
                table: "Subtasks",
                column: "AssignedToId");

            migrationBuilder.AddForeignKey(
                name: "FK_Subtasks_TeamMembers_AssignedToId",
                table: "Subtasks",
                column: "AssignedToId",
                principalTable: "TeamMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Subtasks_TeamMembers_AssignedToId",
                table: "Subtasks");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_Subtasks_AssignedToId",
                table: "Subtasks");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Subtasks");
        }
    }
}
