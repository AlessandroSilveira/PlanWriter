using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyWordLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEvents_Projects_ProjectId",
                table: "ProjectEvents");

            migrationBuilder.DropIndex(
                name: "IX_ProjectEvents_ProjectId_EventId",
                table: "ProjectEvents");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectEvents",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "DailyWordLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WordsWritten = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWordLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyWordLogs_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailyWordLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvents_ProjectId_EventId",
                table: "ProjectEvents",
                columns: new[] { "ProjectId", "EventId" },
                unique: true,
                filter: "[ProjectId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWordLogs_ProjectId_UserId_Date",
                table: "DailyWordLogs",
                columns: new[] { "ProjectId", "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyWordLogs_UserId",
                table: "DailyWordLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEvents_Projects_ProjectId",
                table: "ProjectEvents",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectEvents_Projects_ProjectId",
                table: "ProjectEvents");

            migrationBuilder.DropTable(
                name: "DailyWordLogs");

            migrationBuilder.DropIndex(
                name: "IX_ProjectEvents_ProjectId_EventId",
                table: "ProjectEvents");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectEvents",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEvents_ProjectId_EventId",
                table: "ProjectEvents",
                columns: new[] { "ProjectId", "EventId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectEvents_Projects_ProjectId",
                table: "ProjectEvents",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
