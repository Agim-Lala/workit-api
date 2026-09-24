using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Api.Tests.TestDoubles;
using Workit.Core.Shared.Location;

namespace Workit.Api.Tests;

public sealed class GeocodingEndpointTests
{
    private static readonly AddressSuggestion Tirana = new("Sheshi Skënderbej, Tirana, Albania", 41.3275, 19.8189);

    [Fact]
    public async Task Autocomplete_returns_suggestions_without_authentication()
    {
        await using var factory = CreateFactory(Tirana);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/geocoding/autocomplete?q=Sheshi%20Sk");

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<List<AddressSuggestion>>();
        payload.ShouldNotBeNull();
        payload.ShouldHaveSingleItem().ShouldBe(Tirana);
    }

    [Fact]
    public async Task Autocomplete_rejects_a_too_short_query()
    {
        await using var factory = CreateFactory(Tirana);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/geocoding/autocomplete?q=ab");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Reverse_returns_the_address_at_a_point()
    {
        await using var factory = CreateFactory(Tirana);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/geocoding/reverse?lat=41.3275&lon=19.8189");

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await response.Content.ReadFromJsonAsync<AddressSuggestion>()).ShouldBe(Tirana);
    }

    [Fact]
    public async Task Reverse_returns_not_found_when_no_address_matches()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/geocoding/reverse?lat=0&lon=0");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reverse_rejects_out_of_range_coordinates()
    {
        await using var factory = CreateFactory(Tirana);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/geocoding/reverse?lat=91&lon=19.8189");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static WebApplicationFactory<Program> CreateFactory(AddressSuggestion? result = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAddressLookupService>();
                services.AddSingleton<IAddressLookupService>(new FakeAddressLookupService(result));
            }));
    }
}
