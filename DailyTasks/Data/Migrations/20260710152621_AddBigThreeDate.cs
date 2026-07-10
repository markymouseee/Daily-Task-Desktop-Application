using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyTasks.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBigThreeDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BigThreeDate",
                table: "Tasks",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BigThreeDate",
                table: "Tasks");
        }
    }
}
