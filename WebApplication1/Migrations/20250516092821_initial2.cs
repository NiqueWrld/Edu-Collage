using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
    /// <inheritdoc />
    public partial class initial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClassDay",
                table: "Modules",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ClassTime",
                table: "Modules",
                type: "time",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClassDay",
                table: "Modules");

            migrationBuilder.DropColumn(
                name: "ClassTime",
                table: "Modules");
        }
    }
}
