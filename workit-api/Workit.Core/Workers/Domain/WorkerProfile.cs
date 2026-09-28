using Workit.Core.JobOpenings.Domain;

namespace Workit.Core.Workers.Domain;

public sealed class WorkerProfile
{
    public const int MaxNameLength = 100;
    public const int MaxPhoneLength = 50;
    public const int MaxLocationLength = 200;
    public const int MaxCountryLength = 100;
    public const int MaxOriginalFileNameLength = 255;
    public const int MaxCvTextLength = 50_000;
    public const int MaxInterestedFieldLength = 40;
    public const int MaxInterestedFieldsCount = 15;
    public const int MaxPreferredShiftTypesCount = 3;

    private static readonly IReadOnlyList<string> NoInterestedFields = [];
    private static readonly IReadOnlyList<ShiftType> NoShiftPreferences = [];

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>Storage key for the uploaded CV (see <c>IFileStorage</c>), or null when none was uploaded.</summary>
    public string? CvStorageKey { get; private set; }
    public string? CvOriginalFileName { get; private set; }
    public DateTimeOffset? CvUploadedAt { get; private set; }

    /// <summary>Plain text extracted from the CV PDF, or null when none was uploaded or the PDF had no extractable text (e.g. a scan).</summary>
    public string? CvText { get; private set; }

    // Internal CV-derived profile (see CvReader), used only for ranking — never exposed by the API.
    public IReadOnlyList<string> CvRoles { get; private set; } = NoInterestedFields;
    public IReadOnlyList<string> CvLanguages { get; private set; } = NoInterestedFields;
    public int? CvYearsOfExperience { get; private set; }

    /// <summary>Storage key for the uploaded profile photo, or null when none was uploaded.</summary>
    public string? PhotoStorageKey { get; private set; }
    public DateTimeOffset? PhotoUploadedAt { get; private set; }

    /// <summary>Free-text job fields the worker is interested in, e.g. "bartending", "events".</summary>
    public IReadOnlyList<string> InterestedFields { get; private set; } = NoInterestedFields;

    /// <summary>
    /// Shift types the worker prefers to work, reusing <see cref="JobOpenings.Domain.ShiftType"/>
    /// so preferences line up with the same vocabulary job openings are posted with.
    /// </summary>
    public IReadOnlyList<ShiftType> PreferredShiftTypes { get; private set; } = NoShiftPreferences;

    /// <summary>True when <see cref="Location"/> was confirmed against a real place by the city
    /// lookup service; false for unverified free text or after the location changed again.</summary>
    public bool IsLocationVerified { get; private set; }
    public string? Country { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    private WorkerProfile()
    {
    }

    public WorkerProfile(
        Guid userId,
        string firstName,
        string lastName,
        string location,
        DateTimeOffset createdAt,
        string? phone = null)
    {
        UserId = userId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Location = location.Trim();
        CreatedAt = createdAt;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }

    /// <summary>Sets an unverified free-text location, clearing any previous verification.</summary>
    public void ChangeLocation(string location)
    {
        Location = location.Trim();
        IsLocationVerified = false;
        Country = null;
        Latitude = null;
        Longitude = null;
    }

    /// <summary>Sets a location confirmed by the third-party city lookup service.</summary>
    public void VerifyLocation(string city, string country, double latitude, double longitude)
    {
        Location = city.Trim();
        Country = country.Trim();
        Latitude = latitude;
        Longitude = longitude;
        IsLocationVerified = true;
    }

    public void SetCv(
        string storageKey,
        string originalFileName,
        DateTimeOffset uploadedAt,
        string? text = null,
        IReadOnlyList<string>? roles = null,
        IReadOnlyList<string>? languages = null,
        int? yearsOfExperience = null)
    {
        CvStorageKey = storageKey;
        CvOriginalFileName = originalFileName.Trim();
        CvUploadedAt = uploadedAt;
        var trimmedText = text?.Trim();
        CvText = string.IsNullOrEmpty(trimmedText) ? null : trimmedText[..Math.Min(trimmedText.Length, MaxCvTextLength)];
        CvRoles = roles ?? NoInterestedFields;
        CvLanguages = languages ?? NoInterestedFields;
        CvYearsOfExperience = yearsOfExperience;
    }

    public void SetPhoto(string storageKey, DateTimeOffset uploadedAt)
    {
        PhotoStorageKey = storageKey;
        PhotoUploadedAt = uploadedAt;
    }

    public void SetPreferences(
        IReadOnlyCollection<string> interestedFields,
        IReadOnlyCollection<ShiftType> preferredShiftTypes)
    {
        InterestedFields = interestedFields
            .Select(field => field.Trim())
            .Where(field => field.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        PreferredShiftTypes = preferredShiftTypes.Distinct().ToList();
    }
}
