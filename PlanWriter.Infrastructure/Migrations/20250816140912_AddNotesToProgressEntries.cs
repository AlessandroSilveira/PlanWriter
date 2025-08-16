using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlanWriter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToProgressEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "ProjectProgresses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "ProjectProgresses");
        }
    }
}
