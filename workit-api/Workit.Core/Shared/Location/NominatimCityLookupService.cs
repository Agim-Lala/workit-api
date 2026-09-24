using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Resiliency;

namespace Workit.Core.Shared.Location;

/// <summary>
/// Resolves a free-text city against OpenStreetMap's free Nominatim geocoding API
/// (https://nominatim.org/release-docs/latest/api/Search/). No API key is required, but usage
/// policy requires a descriptive User-Agent (set on the named "Nominatim" <see cref="HttpClient"/>
/// in Program.cs) and asks callers to stay near one request per second.
/// </summary>
public sealed class NominatimCityLookupService(
    HttpClient httpClient,
    IResilienceHandler resilienceHandler,
    ILocalizer localizer,
    ILogger<NominatimCityLookupService> logger) : ICityLookupService
{
    public async Task<CityLookupResult?> FindAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var results = await resilienceHandler.HandleWithRetryAsync(
                async token =>
                {
                    // accept-language keeps the resolved country/city name in the caller's
                    // language instead of Nominatim's default (the place's own local language) —
                    // without it a request in English gets back e.g. "Shqipëria" for Albania.
                    var url = "search"
                        + $"?q={Uri.EscapeDataString(query)}"
                        + $"&accept-language={localizer.CurrentLanguage}"
                        + "&format=jsonv2&addressdetails=1&limit=1";
                    return await httpClient.GetFromJsonAsync<NominatimPlace[]>(url, token);
                },
                cancellationToken);

            var place = results?.FirstOrDefault();
            return place is null ? null : ToResult(place);
        }
        catch (Exception exception)
        {
            // A third-party outage or an unexpected response shape must never block a worker
            // from saving their location preference — it just means it stays unverified.
            logger.LogWarning(exception, "City lookup failed for query {Query}", query);
            return null;
        }
    }

    private static CityLookupResult? ToResult(NominatimPlace place)
    {
        var city = place.Address?.City
            ?? place.Address?.Town
            ?? place.Address?.Village
            ?? place.Address?.Municipality
            ?? place.Address?.County;
        var country = place.Address?.Country;

        if (string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(country)
            || !double.TryParse(place.Lat, CultureInfo.InvariantCulture, out var latitude)
            || !double.TryParse(place.Lon, CultureInfo.InvariantCulture, out var longitude))
        {
            return null;
        }

        return new CityLookupResult(city, country, latitude, longitude);
    }

    private sealed class NominatimPlace
    {
        [JsonPropertyName("lat")]
        public string? Lat { get; init; }

        [JsonPropertyName("lon")]
        public string? Lon { get; init; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; init; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("town")]
        public string? Town { get; init; }

        [JsonPropertyName("village")]
        public string? Village { get; init; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; init; }

        [JsonPropertyName("county")]
        public string? County { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }
    }
}
