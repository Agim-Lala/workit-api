using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Localization;

namespace Workit.Core.Shared.Persistence.Configurations;

public sealed class JobOpeningConfiguration : IEntityTypeConfiguration<JobOpening>
{
    private static readonly JsonSerializerOptions TranslationsJsonOptions = new(JsonSerializerDefaults.Web);

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

        builder.Property(jobOpening => jobOpening.ContentLanguage)
            .HasMaxLength(JobOpening.MaxContentLanguageLength)
            .HasDefaultValue(Language.Default)
            .IsRequired();

        builder.Property(jobOpening => jobOpening.Translations)
            .HasColumnName("Translations")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(
                translations => JsonSerializer.Serialize(translations, TranslationsJsonOptions),
                json => Deserialize(json),
                new ValueComparer<IReadOnlyDictionary<string, JobOpeningTranslation>>(
                    (left, right) => Serialize(left) == Serialize(right),
                    value => Serialize(value).GetHashCode(),
                    value => Deserialize(Serialize(value))))
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

    private static string Serialize(IReadOnlyDictionary<string, JobOpeningTranslation>? translations) =>
        JsonSerializer.Serialize(
            translations ?? new Dictionary<string, JobOpeningTranslation>(),
            TranslationsJsonOptions);

    private static IReadOnlyDictionary<string, JobOpeningTranslation> Deserialize(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, JobOpeningTranslation>()
            : JsonSerializer.Deserialize<Dictionary<string, JobOpeningTranslation>>(json, TranslationsJsonOptions)
              ?? new Dictionary<string, JobOpeningTranslation>();
}
