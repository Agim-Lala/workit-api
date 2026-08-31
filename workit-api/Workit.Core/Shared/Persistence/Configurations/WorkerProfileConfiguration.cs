using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Users.Domain;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class WorkerProfileConfiguration : IEntityTypeConfiguration<WorkerProfile>
{
    public void Configure(EntityTypeBuilder<WorkerProfile> builder)
    {
        builder.ToTable("worker_profiles");
        builder.HasKey(workerProfile => workerProfile.Id);

        builder.Property(workerProfile => workerProfile.FirstName)
            .HasMaxLength(WorkerProfile.MaxNameLength)
            .IsRequired();

        builder.Property(workerProfile => workerProfile.LastName)
            .HasMaxLength(WorkerProfile.MaxNameLength)
            .IsRequired();

        builder.Property(workerProfile => workerProfile.Phone)
            .HasMaxLength(WorkerProfile.MaxPhoneLength);

        builder.Property(workerProfile => workerProfile.CreatedAt)
            .IsRequired();

        builder.HasIndex(workerProfile => workerProfile.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<WorkerProfile>(workerProfile => workerProfile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
