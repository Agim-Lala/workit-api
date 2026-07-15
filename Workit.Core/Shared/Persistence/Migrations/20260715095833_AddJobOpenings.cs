using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workit.Core.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOpenings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_openings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PayAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PayType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ScheduleType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequiredWorkersCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_openings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_openings_business_profiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "business_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_openings_BusinessProfileId",
                table: "job_openings",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_job_openings_StartsAt",
                table: "job_openings",
                column: "StartsAt");

            migrationBuilder.CreateIndex(
                name: "IX_job_openings_Status",
                table: "job_openings",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_openings");
        }
    }
}
