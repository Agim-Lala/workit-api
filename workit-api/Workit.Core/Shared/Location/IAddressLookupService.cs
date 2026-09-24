namespace Workit.Core.Shared.Location;

/// <summary>A human-readable address with the coordinates it resolves to.</summary>
public sealed record AddressSuggestion(string Address, double Latitude, double Longitude);

/// <summary>
/// Address search for the business signup form: autocomplete while the user types, and reverse
/// lookup when they drop a map pin. Like <see cref="ICityLookupService"/>, a third-party outage is
/// a soft outcome (empty list / <c>null</c>), never an exception.
/// </summary>
public interface IAddressLookupService
{
    Task<IReadOnlyList<AddressSuggestion>> AutocompleteAsync(string query, CancellationToken cancellationToken = default);

    Task<AddressSuggestion?> ReverseAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
