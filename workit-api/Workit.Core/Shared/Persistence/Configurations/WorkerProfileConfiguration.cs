using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Users.Domain;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class WorkerProfileConfiguration : IEntityTypeConfiguration<WorkerProfile>
{
    private static readonly JsonSerializerOptions ShiftTypesJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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

        builder.Property(workerProfile => workerProfile.Location)
            .HasMaxLength(WorkerProfile.MaxLocationLength)
            .IsRequired();

        builder.Property(workerProfile => workerProfile.CreatedAt)
            .IsRequired();

        builder.Property(workerProfile => workerProfile.CvStorageKey)
            .HasMaxLength(500);

        builder.Property(workerProfile => workerProfile.CvOriginalFileName)
            .HasMaxLength(WorkerProfile.MaxOriginalFileNameLength);

        builder.Property(workerProfile => workerProfile.PhotoStorageKey)
            .HasMaxLength(500);

        builder.Property(workerProfile => workerProfile.IsLocationVerified)
            .IsRequired();

        builder.Property(workerProfile => workerProfile.Country)
            .HasMaxLength(WorkerProfile.MaxCountryLength);

        builder.Property(workerProfile => workerProfile.Latitude)
            .HasPrecision(9, 6);

        builder.Property(workerProfile => workerProfile.Longitude)
            .HasPrecision(9, 6);

        builder.Property(workerProfile => workerProfile.InterestedFields)
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(workerProfile => workerProfile.PreferredShiftTypes)
            .HasColumnName("PreferredShiftTypes")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .HasConversion(
                shiftTypes => Serialize(shiftTypes),
                json => Deserialize(json),
                new ValueComparer<IReadOnlyList<ShiftType>>(
                    (left, right) => Serialize(left) == Serialize(right),
                    value => Serialize(value).GetHashCode(),
                    value => Deserialize(Serialize(value))))
            .IsRequired();

        builder.HasIndex(workerProfile => workerProfile.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<WorkerProfile>(workerProfile => workerProfile.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static string Serialize(IReadOnlyList<ShiftType>? shiftTypes) =>
        JsonSerializer.Serialize(shiftTypes ?? [], ShiftTypesJsonOptions);

    private static IReadOnlyList<ShiftType> Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<ShiftType>>(json, ShiftTypesJsonOptions) ?? [];
}
