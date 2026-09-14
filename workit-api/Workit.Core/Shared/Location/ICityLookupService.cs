namespace Workit.Core.Shared.Location;

/// <summary>A city resolved and normalized by a third-party place lookup.</summary>
public sealed record CityLookupResult(string City, string Country, double Latitude, double Longitude);

/// <summary>
/// Looks up a free-text place name against a real-world city, used to verify the city a worker
/// enters as a location preference. Implementations should treat "no match" and "the lookup
/// service is unavailable" as the same soft outcome (<c>null</c>) — a third-party outage should
/// never block a worker from saving their location, it only forfeits the verified badge.
/// </summary>
public interface ICityLookupService
{
    Task<CityLookupResult?> FindAsync(string query, CancellationToken cancellationToken = default);
}
