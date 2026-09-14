using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workit.Core.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOpeningTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentLanguage",
                table: "job_openings",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<string>(
                name: "Translations",
                table: "job_openings",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentLanguage",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "Translations",
                table: "job_openings");
        }
    }
}
