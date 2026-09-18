using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class WorkerVerificationConfiguration : IEntityTypeConfiguration<WorkerVerification>
{
    public void Configure(EntityTypeBuilder<WorkerVerification> builder)
    {
        builder.ToTable("worker_verifications");
        builder.HasKey(verification => verification.Id);

        builder.Property(verification => verification.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(verification => verification.Provider)
            .HasMaxLength(WorkerVerification.MaxProviderLength)
            .IsRequired();

        builder.Property(verification => verification.ProviderReferenceId)
            .HasMaxLength(WorkerVerification.MaxProviderReferenceIdLength)
            .IsRequired();

        builder.Property(verification => verification.RejectionReason)
            .HasMaxLength(WorkerVerification.MaxRejectionReasonLength);

        builder.Property(verification => verification.CreatedAt)
            .IsRequired();

        builder.HasIndex(verification => verification.WorkerProfileId)
            .IsUnique();

        builder.HasIndex(verification => verification.ProviderReferenceId);

        builder.HasOne<WorkerProfile>()
            .WithOne()
            .HasForeignKey<WorkerVerification>(verification => verification.WorkerProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
