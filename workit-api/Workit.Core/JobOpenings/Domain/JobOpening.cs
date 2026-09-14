using Workit.Core.Shared.Localization;

namespace Workit.Core.JobOpenings.Domain;

public sealed class JobOpening
{
    public const int MaxTitleLength = 160;
    public const int MaxDescriptionLength = 4000;
    public const int MaxRoleLength = 120;
    public const int MaxLocationLength = 500;
    public const int MaxContentLanguageLength = 8;

    private static readonly IReadOnlyDictionary<string, JobOpeningTranslation> NoTranslations =
        new Dictionary<string, JobOpeningTranslation>();

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BusinessProfileId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public decimal PayAmount { get; private set; }
    public PayType PayType { get; private set; }
    public JobType JobType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public ShiftType ShiftType { get; private set; }
    public TimeOnly? ShiftStartTime { get; private set; }
    public TimeOnly? ShiftEndTime { get; private set; }
    public int RequiredWorkersCount { get; private set; }
    public JobOpeningStatus Status { get; private set; } = JobOpeningStatus.Open;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>Language the base <see cref="Title"/>, <see cref="Description"/> and <see cref="Role"/> are written in.</summary>
    public string ContentLanguage { get; private set; } = Language.Default;

    /// <summary>Optional per-language translations of the free-text fields, keyed by two-letter language code.</summary>
    public IReadOnlyDictionary<string, JobOpeningTranslation> Translations { get; private set; } = NoTranslations;

    private JobOpening()
    {
    }

    public JobOpening(
        Guid businessProfileId,
        string title,
        string description,
        string role,
        string location,
        decimal payAmount,
        PayType payType,
        JobType jobType,
        DateOnly startDate,
        DateOnly? endDate,
        ShiftType shiftType,
        TimeOnly? shiftStartTime,
        TimeOnly? shiftEndTime,
        int requiredWorkersCount,
        DateTimeOffset createdAt,
        string? contentLanguage = null,
        IReadOnlyDictionary<string, JobOpeningTranslation>? translations = null)
    {
        ValidateSchedule(jobType, startDate, endDate, shiftType, shiftStartTime, shiftEndTime);

        BusinessProfileId = businessProfileId;
        Title = title.Trim();
        Description = description.Trim();
        Role = role.Trim();
        Location = location.Trim();
        PayAmount = payAmount;
        PayType = payType;
        JobType = jobType;
        StartDate = startDate;
        EndDate = endDate;
        ShiftType = shiftType;
        ShiftStartTime = shiftStartTime;
        ShiftEndTime = shiftEndTime;
        RequiredWorkersCount = requiredWorkersCount;
        CreatedAt = createdAt;
        ContentLanguage = Language.Resolve(contentLanguage);
        Translations = NormalizeTranslations(translations);
    }

    /// <summary>
    /// Returns the free-text content for <paramref name="language"/>, falling back field by field
    /// to the base-language values when a translation is missing or blank.
    /// </summary>
    public JobOpeningContent ResolveContent(string language)
    {
        var resolvedLanguage = Language.Resolve(language);

        if (resolvedLanguage == ContentLanguage
            || !Translations.TryGetValue(resolvedLanguage, out var translation))
        {
            return new JobOpeningContent(Title, Description, Role);
        }

        return new JobOpeningContent(
            Coalesce(translation.Title, Title),
            Coalesce(translation.Description, Description),
            Coalesce(translation.Role, Role));

        static string Coalesce(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private IReadOnlyDictionary<string, JobOpeningTranslation> NormalizeTranslations(
        IReadOnlyDictionary<string, JobOpeningTranslation>? source)
    {
        if (source is null || source.Count == 0)
        {
            return NoTranslations;
        }

        var normalized = new Dictionary<string, JobOpeningTranslation>();
        foreach (var (language, translation) in source)
        {
            var resolvedLanguage = Language.Resolve(language);
            if (resolvedLanguage == ContentLanguage || translation is null)
            {
                continue;
            }

            normalized[resolvedLanguage] = new JobOpeningTranslation(
                Trim(translation.Title),
                Trim(translation.Description),
                Trim(translation.Role));
        }

        return normalized.Count == 0 ? NoTranslations : normalized;

        static string? Trim(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void ValidateSchedule(
        JobType jobType,
        DateOnly startDate,
        DateOnly? endDate,
        ShiftType shiftType,
        TimeOnly? shiftStartTime,
        TimeOnly? shiftEndTime)
    {
        if (jobType == JobType.Permanent && endDate.HasValue)
        {
            throw new ArgumentException("Permanent jobs cannot have an end date.", nameof(endDate));
        }

        if (jobType != JobType.Permanent && !endDate.HasValue)
        {
            throw new ArgumentException("Project and short-term jobs require an end date.", nameof(endDate));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("The end date cannot be before the start date.", nameof(endDate));
        }

        var hasBothCustomTimes = shiftStartTime.HasValue && shiftEndTime.HasValue;
        if (shiftType == ShiftType.CustomHours
            && (!hasBothCustomTimes || shiftStartTime == shiftEndTime))
        {
            throw new ArgumentException(
                "Custom-hours shifts require different start and end times.",
                nameof(shiftStartTime));
        }

        if (shiftType != ShiftType.CustomHours
            && (shiftStartTime.HasValue || shiftEndTime.HasValue))
        {
            throw new ArgumentException(
                "Morning and evening shifts cannot include custom hours.",
                nameof(shiftStartTime));
        }
    }
}
