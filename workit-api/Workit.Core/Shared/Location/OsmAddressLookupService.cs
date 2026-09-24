using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Resiliency;

namespace Workit.Core.Shared.Location;

/// <summary>
/// Free OpenStreetMap-backed address lookup. Autocomplete uses Photon (https://photon.komoot.io),
/// because Nominatim's usage policy forbids search-as-you-type; reverse lookup uses Nominatim.
/// Both are called by absolute URL from one <see cref="HttpClient"/> carrying the User-Agent
/// Nominatim requires (see DependencyInjection).
/// </summary>
public sealed class OsmAddressLookupService(
    HttpClient httpClient,
    IResilienceHandler resilienceHandler,
    ILocalizer localizer,
    ILogger<OsmAddressLookupService> logger) : IAddressLookupService
{
    private const int MaxSuggestions = 5;

    public async Task<IReadOnlyList<AddressSuggestion>> AutocompleteAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Photon only knows default/en/de/fr; "default" returns local names, which for
            // Albanian addresses is already Albanian.
            var language = localizer.CurrentLanguage == Language.English ? "&lang=en" : string.Empty;
            var url = "https://photon.komoot.io/api/"
                + $"?q={Uri.EscapeDataString(query)}"
                + $"&limit={MaxSuggestions}"
                + language;

            var response = await resilienceHandler.HandleWithRetryAsync(
                async token => await httpClient.GetFromJsonAsync<PhotonResponse>(url, token),
                cancellationToken);

            return response?.Features?
                .Select(ToSuggestion)
                .OfType<AddressSuggestion>()
                // A long street comes back as one feature per OSM segment, all with the same label.
                .DistinctBy(suggestion => suggestion.Address)
                .ToList() ?? [];
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Address autocomplete failed for query {Query}", query);
            return [];
        }
    }

    public async Task<AddressSuggestion?> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "https://nominatim.openstreetmap.org/reverse"
                + $"?lat={latitude.ToString(CultureInfo.InvariantCulture)}"
                + $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}"
                + $"&accept-language={localizer.CurrentLanguage}"
                + "&format=jsonv2";

            var place = await resilienceHandler.HandleWithRetryAsync(
                async token => await httpClient.GetFromJsonAsync<NominatimPlace>(url, token),
                cancellationToken);

            // Nominatim answers an unmatched point with 200 and {"error": "..."}, so no display_name.
            return string.IsNullOrWhiteSpace(place?.DisplayName)
                || !double.TryParse(place.Lat, CultureInfo.InvariantCulture, out var placeLatitude)
                || !double.TryParse(place.Lon, CultureInfo.InvariantCulture, out var placeLongitude)
                ? null
                : new AddressSuggestion(place.DisplayName, placeLatitude, placeLongitude);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Reverse address lookup failed for {Latitude},{Longitude}", latitude, longitude);
            return null;
        }
    }

    private static AddressSuggestion? ToSuggestion(PhotonFeature feature)
    {
        // GeoJSON order is [longitude, latitude].
        if (feature.Geometry?.Coordinates is not [var longitude, var latitude, ..] || feature.Properties is null)
        {
            return null;
        }

        var properties = feature.Properties;
        var street = string.Join(' ', new[] { properties.Street, properties.HouseNumber }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var parts = new[] { properties.Name, street, properties.City, properties.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct();
        var address = string.Join(", ", parts);

        return string.IsNullOrWhiteSpace(address) ? null : new AddressSuggestion(address, latitude, longitude);
    }

    private sealed class PhotonResponse
    {
        [JsonPropertyName("features")]
        public PhotonFeature[]? Features { get; init; }
    }

    private sealed class PhotonFeature
    {
        [JsonPropertyName("geometry")]
        public PhotonGeometry? Geometry { get; init; }

        [JsonPropertyName("properties")]
        public PhotonProperties? Properties { get; init; }
    }

    private sealed class PhotonGeometry
    {
        [JsonPropertyName("coordinates")]
        public double[]? Coordinates { get; init; }
    }

    private sealed class PhotonProperties
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("street")]
        public string? Street { get; init; }

        [JsonPropertyName("housenumber")]
        public string? HouseNumber { get; init; }

        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }
    }

    private sealed class NominatimPlace
    {
        [JsonPropertyName("lat")]
        public string? Lat { get; init; }

        [JsonPropertyName("lon")]
        public string? Lon { get; init; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }
    }
}
