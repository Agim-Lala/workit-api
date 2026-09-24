using Workit.Core.Shared.Location;

namespace Workit.Api.Tests.TestDoubles;

/// <summary>
/// Test double for <see cref="IAddressLookupService"/> so API tests never call Photon/Nominatim.
/// Returns the given suggestion for both lookups, or nothing when constructed without one.
/// </summary>
public sealed class FakeAddressLookupService(AddressSuggestion? result = null) : IAddressLookupService
{
    public Task<IReadOnlyList<AddressSuggestion>> AutocompleteAsync(string query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AddressSuggestion>>(result is null ? [] : [result]);
    }

    public Task<AddressSuggestion?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(result);
    }
}
