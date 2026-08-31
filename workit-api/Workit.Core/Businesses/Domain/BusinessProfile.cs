namespace Workit.Core.Businesses.Domain;

public sealed class BusinessProfile
{
    public const int MaxBusinessNameLength = 255;
    public const int MaxFullAddressLength = 500;
    public const int MaxPhoneLength = 50;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string BusinessName { get; private set; } = string.Empty;
    public string FullAddress { get; private set; } = string.Empty;
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public string? Phone { get; private set; }
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
        DateTimeOffset createdAt,
        string? phone = null)
    {
        UserId = userId;
        BusinessName = businessName.Trim();
        FullAddress = fullAddress.Trim();
        Latitude = latitude;
        Longitude = longitude;
        CreatedAt = createdAt;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
