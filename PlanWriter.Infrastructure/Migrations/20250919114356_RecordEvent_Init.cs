using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecordEvent_Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ValidatedWords",
                table: "ProjectEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationSource",
                table: "ProjectEvents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidatedWords",
                table: "ProjectEvents");

            migrationBuilder.DropColumn(
                name: "ValidationSource",
                table: "ProjectEvents");
        }
    }
}
