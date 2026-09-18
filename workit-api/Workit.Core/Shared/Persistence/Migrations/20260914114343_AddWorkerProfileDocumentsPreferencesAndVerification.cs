using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workit.Core.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkerProfileDocumentsPreferencesAndVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "worker_profiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CvOriginalFileName",
                table: "worker_profiles",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CvStorageKey",
                table: "worker_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CvUploadedAt",
                table: "worker_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "InterestedFields",
                table: "worker_profiles",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocationVerified",
                table: "worker_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "worker_profiles",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "worker_profiles",
                type: "double precision",
                precision: 9,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoStorageKey",
                table: "worker_profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PhotoUploadedAt",
                table: "worker_profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredShiftTypes",
                table: "worker_profiles",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateTable(
                name: "worker_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProviderReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_worker_verifications_worker_profiles_WorkerProfileId",
                        column: x => x.WorkerProfileId,
                        principalTable: "worker_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_verifications_ProviderReferenceId",
                table: "worker_verifications",
                column: "ProviderReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_worker_verifications_WorkerProfileId",
                table: "worker_verifications",
                column: "WorkerProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_verifications");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "CvOriginalFileName",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "CvStorageKey",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "CvUploadedAt",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "InterestedFields",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "IsLocationVerified",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "PhotoStorageKey",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "PhotoUploadedAt",
                table: "worker_profiles");

            migrationBuilder.DropColumn(
                name: "PreferredShiftTypes",
                table: "worker_profiles");
        }
    }
}
