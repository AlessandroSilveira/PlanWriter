using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "CoverBytes",
                table: "Projects",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverMime",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverSize",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CoverUpdatedAt",
                table: "Projects",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverBytes",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CoverMime",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CoverSize",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CoverUpdatedAt",
                table: "Projects");
        }
    }
}
