using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workit.Core.Shared.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobTypesAndShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_openings_StartsAt",
                table: "job_openings");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "job_openings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobType",
                table: "job_openings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ShiftEndTime",
                table: "job_openings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "ShiftStartTime",
                table: "job_openings",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShiftType",
                table: "job_openings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly?>(
                name: "StartDate",
                table: "job_openings",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE job_openings
                SET "JobType" = CASE
                        WHEN "ScheduleType" = 'LongTerm' THEN 'Permanent'
                        WHEN "ScheduleType" = 'DateRange' THEN 'Project'
                        ELSE 'ShortTerm'
                    END,
                    "StartDate" = ("StartsAt" AT TIME ZONE 'UTC')::date,
                    "EndDate" = CASE
                        WHEN "ScheduleType" = 'LongTerm' THEN NULL
                        ELSE COALESCE(
                            ("EndsAt" AT TIME ZONE 'UTC')::date,
                            ("StartsAt" AT TIME ZONE 'UTC')::date)
                    END,
                    "ShiftType" = 'CustomHours',
                    "ShiftStartTime" = ("StartsAt" AT TIME ZONE 'UTC')::time,
                    "ShiftEndTime" = COALESCE(
                        ("EndsAt" AT TIME ZONE 'UTC')::time,
                        (("StartsAt" AT TIME ZONE 'UTC') + INTERVAL '8 hours')::time);
                """);

            migrationBuilder.AlterColumn<string>(
                name: "JobType",
                table: "job_openings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShiftType",
                table: "job_openings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "job_openings",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "ScheduleType",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "StartsAt",
                table: "job_openings");

            migrationBuilder.CreateIndex(
                name: "IX_job_openings_JobType_StartDate_EndDate",
                table: "job_openings",
                columns: new[] { "JobType", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_job_openings_StartDate",
                table: "job_openings",
                column: "StartDate");

            migrationBuilder.AddCheckConstraint(
                name: "CK_job_openings_date_range",
                table: "job_openings",
                sql: "(\"JobType\" = 'Permanent' AND \"EndDate\" IS NULL) OR (\"JobType\" IN ('Project', 'ShortTerm') AND \"EndDate\" IS NOT NULL AND \"EndDate\" >= \"StartDate\")");

            migrationBuilder.AddCheckConstraint(
                name: "CK_job_openings_shift",
                table: "job_openings",
                sql: "(\"ShiftType\" = 'CustomHours' AND \"ShiftStartTime\" IS NOT NULL AND \"ShiftEndTime\" IS NOT NULL AND \"ShiftStartTime\" <> \"ShiftEndTime\") OR (\"ShiftType\" IN ('Morning', 'Evening') AND \"ShiftStartTime\" IS NULL AND \"ShiftEndTime\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_job_openings_JobType_StartDate_EndDate",
                table: "job_openings");

            migrationBuilder.DropIndex(
                name: "IX_job_openings_StartDate",
                table: "job_openings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_job_openings_date_range",
                table: "job_openings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_job_openings_shift",
                table: "job_openings");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndsAt",
                table: "job_openings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScheduleType",
                table: "job_openings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset?>(
                name: "StartsAt",
                table: "job_openings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE job_openings
                SET "ScheduleType" = CASE
                        WHEN "JobType" = 'Permanent' THEN 'LongTerm'
                        WHEN "JobType" = 'Project' THEN 'DateRange'
                        ELSE 'SpecificDates'
                    END,
                    "StartsAt" = (
                        "StartDate" + CASE "ShiftType"
                            WHEN 'Morning' THEN TIME '08:00'
                            WHEN 'Evening' THEN TIME '16:00'
                            ELSE "ShiftStartTime"
                        END
                    ) AT TIME ZONE 'UTC',
                    "EndsAt" = CASE
                        WHEN "JobType" = 'Permanent' THEN NULL
                        ELSE (
                            "EndDate" + CASE "ShiftType"
                                WHEN 'Morning' THEN TIME '16:00'
                                WHEN 'Evening' THEN TIME '00:00'
                                ELSE "ShiftEndTime"
                            END
                            + CASE
                                WHEN CASE "ShiftType"
                                        WHEN 'Morning' THEN TIME '16:00'
                                        WHEN 'Evening' THEN TIME '00:00'
                                        ELSE "ShiftEndTime"
                                    END <= CASE "ShiftType"
                                        WHEN 'Morning' THEN TIME '08:00'
                                        WHEN 'Evening' THEN TIME '16:00'
                                        ELSE "ShiftStartTime"
                                    END
                                THEN INTERVAL '1 day'
                                ELSE INTERVAL '0 days'
                            END
                        ) AT TIME ZONE 'UTC'
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ScheduleType",
                table: "job_openings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartsAt",
                table: "job_openings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "JobType",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "ShiftEndTime",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "ShiftStartTime",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "ShiftType",
                table: "job_openings");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "job_openings");

            migrationBuilder.CreateIndex(
                name: "IX_job_openings_StartsAt",
                table: "job_openings",
                column: "StartsAt");
        }
    }
}
