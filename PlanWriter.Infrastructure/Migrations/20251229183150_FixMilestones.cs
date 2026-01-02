using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Milestones_ProjectId_Name",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_ProjectId_Order",
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

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Milestones",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Milestones",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Milestones");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Milestones",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Milestones",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId_Name",
                table: "Milestones",
                columns: new[] { "ProjectId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ProjectId_Order",
                table: "Milestones",
                columns: new[] { "ProjectId", "Order" });
        }
    }
}
