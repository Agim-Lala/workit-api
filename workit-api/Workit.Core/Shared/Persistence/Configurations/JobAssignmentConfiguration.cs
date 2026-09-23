using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Businesses.Domain;
using Workit.Core.Hiring.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class JobAssignmentConfiguration : IEntityTypeConfiguration<JobAssignment>
{
    public void Configure(EntityTypeBuilder<JobAssignment> builder)
    {
        builder.ToTable("job_assignments");
        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(assignment => assignment.HiredAt)
            .IsRequired();

        builder.HasIndex(assignment => assignment.JobOpeningId);
        builder.HasIndex(assignment => assignment.BusinessProfileId);
        builder.HasIndex(assignment => assignment.WorkerProfileId);

        builder.HasOne<JobOpening>()
            .WithMany()
            .HasForeignKey(assignment => assignment.JobOpeningId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BusinessProfile>()
            .WithMany()
            .HasForeignKey(assignment => assignment.BusinessProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkerProfile>()
            .WithMany()
            .HasForeignKey(assignment => assignment.WorkerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
