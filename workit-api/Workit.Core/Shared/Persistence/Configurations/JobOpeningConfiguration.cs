using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class JobOpeningConfiguration : IEntityTypeConfiguration<JobOpening>
{
    public void Configure(EntityTypeBuilder<JobOpening> builder)
    {
        builder.ToTable("job_openings", table =>
        {
            table.HasCheckConstraint(
                "CK_job_openings_date_range",
                "(\"JobType\" = 'Permanent' AND \"EndDate\" IS NULL) OR "
                + "(\"JobType\" IN ('Project', 'ShortTerm') AND \"EndDate\" IS NOT NULL "
                + "AND \"EndDate\" >= \"StartDate\")");
            table.HasCheckConstraint(
                "CK_job_openings_shift",
                "(\"ShiftType\" = 'CustomHours' AND \"ShiftStartTime\" IS NOT NULL "
                + "AND \"ShiftEndTime\" IS NOT NULL AND \"ShiftStartTime\" <> \"ShiftEndTime\") OR "
                + "(\"ShiftType\" IN ('Morning', 'Evening') AND \"ShiftStartTime\" IS NULL "
                + "AND \"ShiftEndTime\" IS NULL)");
        });
        builder.HasKey(jobOpening => jobOpening.Id);

        builder.Property(jobOpening => jobOpening.Title)
            .HasMaxLength(JobOpening.MaxTitleLength)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.Description)
            .HasMaxLength(JobOpening.MaxDescriptionLength)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.Role)
            .HasMaxLength(JobOpening.MaxRoleLength)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.Location)
            .HasMaxLength(JobOpening.MaxLocationLength)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.PayAmount)
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.PayType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.JobType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.StartDate)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.ShiftType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.RequiredWorkersCount)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.CreatedAt)
            .IsRequired();

        builder.HasIndex(jobOpening => jobOpening.BusinessProfileId);
        builder.HasIndex(jobOpening => jobOpening.Status);
        builder.HasIndex(jobOpening => jobOpening.StartDate);
        builder.HasIndex(jobOpening => new
        {
            jobOpening.JobType,
            jobOpening.StartDate,
            jobOpening.EndDate
        });

        builder.HasOne<BusinessProfile>()
            .WithMany()
            .HasForeignKey(jobOpening => jobOpening.BusinessProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
