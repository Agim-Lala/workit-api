namespace Workit.Core.Businesses.Domain;

public sealed class BusinessProfile
{
    public const int MaxBusinessNameLength = 255;
    public const int MaxFullAddressLength = 500;
    public const int MaxPhoneLength = 50;
    public const int NiptLength = 10;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string FullAddress { get; private set; } = string.Empty;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string? Phone { get; private set; }

    /// <summary>
    /// The business's NIPT (Numri i Identifikimit për Personin e Tatueshëm) — Albania's unique tax
    /// registration number, required at signup as the business's proof of a real, registered legal
    /// entity. Normalized to uppercase and enforced unique across businesses.
    /// </summary>
    public string Nipt { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private BusinessProfile()
    {
    }

    public BusinessProfile(
        Guid userId,
        string businessName,
        string fullAddress,
        decimal latitude,
        decimal longitude,
        string nipt,
        DateTimeOffset createdAt,
        string? phone = null)
    {
        UserId = userId;
        BusinessName = businessName.Trim();
        FullAddress = fullAddress.Trim();
        Latitude = latitude;
        Longitude = longitude;
        Nipt = NormalizeNipt(nipt);
        CreatedAt = createdAt;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }

    public static string NormalizeNipt(string nipt)
    {
        return nipt.Trim().ToUpperInvariant();
    }
}
