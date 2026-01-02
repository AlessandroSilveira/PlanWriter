using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Milestones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AddColumn<bool>(
                name: "Completed",
                table: "Milestones",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Milestones",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId_Name",
                table: "Milestones",
                columns: new[] { "ProjectId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId_Order",
                table: "Milestones",
                columns: new[] { "ProjectId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Milestones_ProjectId_Name",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_ProjectId_Order",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "Completed",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Milestones");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Milestones",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
