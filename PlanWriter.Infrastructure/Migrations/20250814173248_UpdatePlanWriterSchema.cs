using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlanWriterSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalTimeSpent",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "TotalWordsGoal",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Projects",
                newName: "UserId");

            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WordCountGoal",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectProgresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalWordsWritten = table.Column<int>(type: "int", nullable: false),
                    RemainingWords = table.Column<int>(type: "int", nullable: false),
                    RemainingPercentage = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeSpentInMinutes = table.Column<int>(type: "int", nullable: false),
                    WordsWritten = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectProgresses_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProgresses_ProjectId",
                table: "ProjectProgresses",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectProgresses");

            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "WordCountGoal",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Projects",
                newName: "Name");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "TotalTimeSpent",
                table: "Projects",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "TotalWordsGoal",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
