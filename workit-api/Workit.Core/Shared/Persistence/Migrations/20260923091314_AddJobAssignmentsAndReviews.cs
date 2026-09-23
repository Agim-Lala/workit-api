using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workit.Core.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobAssignmentsAndReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobOpeningId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HiredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_assignments_business_profiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "business_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_assignments_job_openings_JobOpeningId",
                        column: x => x.JobOpeningId,
                        principalTable: "job_openings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_assignments_worker_profiles_WorkerProfileId",
                        column: x => x.WorkerProfileId,
                        principalTable: "worker_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.Id);
                    table.CheckConstraint("CK_reviews_rating_range", "\"Rating\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_reviews_job_assignments_JobAssignmentId",
                        column: x => x.JobAssignmentId,
                        principalTable: "job_assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_assignments_BusinessProfileId",
                table: "job_assignments",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_job_assignments_JobOpeningId",
                table: "job_assignments",
                column: "JobOpeningId");

            migrationBuilder.CreateIndex(
                name: "IX_job_assignments_WorkerProfileId",
                table: "job_assignments",
                column: "WorkerProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_JobAssignmentId_ReviewerRole",
                table: "reviews",
                columns: new[] { "JobAssignmentId", "ReviewerRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "job_assignments");
        }
    }
}
