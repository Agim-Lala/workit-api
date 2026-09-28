using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workit.Core.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerCvProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "CvLanguages",
                table: "worker_profiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string[]>(
                name: "CvRoles",
                table: "worker_profiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<int>(
                name: "CvYearsOfExperience",
                table: "worker_profiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CvLanguages",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "CvRoles",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "CvYearsOfExperience",
                table: "worker_profiles");
        }
    }
}
