using Workit.Core.Shared.Location;

namespace Workit.Api.Tests.TestDoubles;

/// <summary>
/// Test double for <see cref="ICityLookupService"/> so API tests never make a live call to the
/// real Nominatim service. Defaults to "not found" (mirrors the pre-verification behavior:
/// the free-text location is kept as typed); construct with a <see cref="CityLookupResult"/> to
/// exercise the verified path instead.
/// </summary>
public sealed class FakeCityLookupService(CityLookupResult? result = null) : ICityLookupService
{
    public Task<CityLookupResult?> FindAsync(string query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(result);
    }
}
