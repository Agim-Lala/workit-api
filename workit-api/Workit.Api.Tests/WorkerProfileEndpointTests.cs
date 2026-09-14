using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Api.Tests.TestDoubles;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Location;
using Workit.Core.Shared.Persistence;
using Workit.Core.Workers;
using Workit.Core.Workers.Domain;

namespace Workit.Api.Tests;

public sealed class WorkerProfileEndpointTests
{
    private const string WebhookSecret = "test-persona-webhook-secret";

    [Fact]
    public async Task Update_preferences_returns_and_persists_saved_values()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);

        var response = await client.PutAsJsonAsync(
            "/worker-profile/preferences",
            new { interestedFields = new[] { "Bartending", "Events" }, preferredShiftTypes = new[] { ShiftType.Morning } });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<UpdateWorkerPreferences.Response>();
        payload.ShouldNotBeNull();
        payload.InterestedFields.ShouldBe(["Bartending", "Events"]);
        payload.PreferredShiftTypes.ShouldBe([ShiftType.Morning]);

        var profile = await GetProfileAsync(client);
        profile.InterestedFields.ShouldBe(["Bartending", "Events"]);
        profile.PreferredShiftTypes.ShouldBe([ShiftType.Morning]);
    }

    [Fact]
    public async Task Update_preferences_rejects_too_many_interested_fields()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);
        var tooMany = Enumerable.Range(1, WorkerProfile.MaxInterestedFieldsCount + 1).Select(i => $"field{i}").ToArray();

        var response = await client.PutAsJsonAsync(
            "/worker-profile/preferences",
            new { interestedFields = tooMany, preferredShiftTypes = Array.Empty<ShiftType>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_and_download_cv_round_trips_the_file()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);
        var bytes = "%PDF-1.4 fake cv content"u8.ToArray();

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        uploadContent.Add(fileContent, "file", "my-cv.pdf");
        var uploadResponse = await client.PostAsync("/worker-profile/cv", uploadContent);

        uploadResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await uploadResponse.Content.ReadAsStringAsync());
        var uploadPayload = await uploadResponse.Content.ReadFromJsonAsync<UploadWorkerCv.Response>();
        uploadPayload.ShouldNotBeNull();
        uploadPayload.FileName.ShouldBe("my-cv.pdf");

        var profile = await GetProfileAsync(client);
        profile.HasCv.ShouldBeTrue();
        profile.CvFileName.ShouldBe("my-cv.pdf");

        var downloadResponse = await client.GetAsync("/worker-profile/cv");
        downloadResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        var downloaded = await downloadResponse.Content.ReadAsByteArrayAsync();
        downloaded.ShouldBe(bytes);
    }

    [Fact]
    public async Task Upload_cv_rejects_a_non_pdf_file()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("not a pdf"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        uploadContent.Add(fileContent, "file", "notes.txt");

        var response = await client.PostAsync("/worker-profile/cv", uploadContent);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_and_download_photo_round_trips_the_file()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);
        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3 };

        using var uploadContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(fileContent, "file", "photo.jpg");
        var uploadResponse = await client.PostAsync("/worker-profile/photo", uploadContent);

        uploadResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await uploadResponse.Content.ReadAsStringAsync());

        var profile = await GetProfileAsync(client);
        profile.HasPhoto.ShouldBeTrue();

        var downloadResponse = await client.GetAsync("/worker-profile/photo");
        downloadResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType!.MediaType.ShouldBe("image/jpeg");
        (await downloadResponse.Content.ReadAsByteArrayAsync()).ShouldBe(bytes);
    }

    [Fact]
    public async Task Update_location_marks_it_verified_when_the_city_lookup_matches()
    {
        var lookupResult = new CityLookupResult("Tirana", "Albania", 41.3275, 19.8189);
        await using var factory = CreateFactory(cityLookupResult: lookupResult);
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);

        var response = await client.PutAsJsonAsync("/worker-profile/location", new { location = "tirana" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<UpdateWorkerLocation.Response>();
        payload.ShouldNotBeNull();
        payload.IsLocationVerified.ShouldBeTrue();
        payload.Location.ShouldBe("Tirana");
        payload.Country.ShouldBe("Albania");
    }

    [Fact]
    public async Task Update_location_keeps_raw_text_unverified_when_the_city_lookup_finds_nothing()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);

        var response = await client.PutAsJsonAsync("/worker-profile/location", new { location = "Somewhereville" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<UpdateWorkerLocation.Response>();
        payload.ShouldNotBeNull();
        payload.IsLocationVerified.ShouldBeFalse();
        payload.Location.ShouldBe("Somewhereville");
    }

    [Fact]
    public async Task Start_verification_returns_a_hosted_url_and_marks_the_badge_pending()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);

        var response = await client.PostAsync("/worker-profile/verification/start", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<StartWorkerVerification.Response>();
        payload.ShouldNotBeNull();
        payload.HostedUrl.ShouldNotBeNullOrWhiteSpace();
        payload.Status.ShouldBe(WorkerVerificationStatus.Pending);

        var profile = await GetProfileAsync(client);
        profile.VerificationStatus.ShouldBe(WorkerVerificationStatus.Pending);
    }

    [Fact]
    public async Task Persona_webhook_marks_the_worker_verified_when_the_signature_is_valid()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        await AuthenticateAsWorkerAsync(client);
        await client.PostAsync("/worker-profile/verification/start", content: null);
        var inquiryId = await GetProviderReferenceIdAsync(factory);

        var body = "{\"data\":{\"attributes\":{\"payload\":{\"data\":{\"id\":\"" + inquiryId
            + "\",\"attributes\":{\"status\":\"approved\"}}}}}}";
        using var request = SignedWebhookRequest(body);

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await GetProfileAsync(client);
        profile.VerificationStatus.ShouldBe(WorkerVerificationStatus.Verified);
    }

    [Fact]
    public async Task Persona_webhook_rejects_an_invalid_signature()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var body = """{"data":{"attributes":{"payload":{"data":{"id":"inq_123","attributes":{"status":"approved"}}}}}}""";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/persona")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Persona-Signature", "t=1,v1=deadbeef");

        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static HttpRequestMessage SignedWebhookRequest(string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{body}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(WebhookSecret), Encoding.UTF8.GetBytes(signedPayload));
        var signature = $"t={timestamp},v1={Convert.ToHexStringLower(hash)}";

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/persona")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Persona-Signature", signature);
        return request;
    }

    private static async Task<string> GetProviderReferenceIdAsync(WebApplicationFactory<Program> factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var verification = await db.Set<WorkerVerification>().SingleAsync();
        return verification.ProviderReferenceId;
    }

    private static async Task AuthenticateAsWorkerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("worker@example.com", "password123", "Test", "Worker", "Tirana"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        payload.ShouldNotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
    }

    private static async Task<GetWorkerProfile.Response> GetProfileAsync(HttpClient client)
    {
        var response = await client.GetAsync("/worker-profile");
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<GetWorkerProfile.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static WebApplicationFactory<Program> CreateFactory(CityLookupResult? cityLookupResult = null)
    {
        var databaseName = $"workit-worker-profile-tests-{Guid.NewGuid()}";
        var databaseRoot = new InMemoryDatabaseRoot();
        var inMemoryProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.RemoveAll<DbContextOptions<ReadAppDbContext>>();
                    services.RemoveAll<DbContextOptions>();

                    services.AddDbContext<AppDbContext>(options => options
                        .UseInMemoryDatabase(databaseName, databaseRoot)
                        .UseInternalServiceProvider(inMemoryProvider));
                    services.AddDbContext<ReadAppDbContext>(options => options
                        .UseInMemoryDatabase(databaseName, databaseRoot)
                        .UseInternalServiceProvider(inMemoryProvider));

                    // Never let tests reach the real Nominatim/Persona third parties.
                    services.RemoveAll<ICityLookupService>();
                    services.AddSingleton<ICityLookupService>(new FakeCityLookupService(cityLookupResult));
                    services.RemoveAll<IIdentityVerificationProvider>();
                    services.AddSingleton<IIdentityVerificationProvider, FakeIdentityVerificationProvider>();

                    // Give the webhook endpoint a known secret to sign test requests with.
                    services.RemoveAll<WorkitSettings>();
                    var settings = WorkitSettings.FromEnvironment();
                    services.AddSingleton(settings with { Persona = settings.Persona with { WebhookSecret = WebhookSecret } });
                });
            });
    }
}
