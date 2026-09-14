using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Api.JobOpenings;
using Workit.Core.Businesses;
using Workit.Core.JobOpenings;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Persistence;
using Workit.Core.Workers;

namespace Workit.Api.Tests;

public sealed class LocalizationEndpointTests
{
    [Fact]
    public async Task Validation_errors_are_returned_in_english_by_default()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("no-location@example.com", "password123", "Test", "Worker", ""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("Validation failed");
        problem.Errors["Location"][0].ShouldBe("'Location' must not be empty.");
    }

    [Fact]
    public async Task Validation_errors_are_translated_when_accept_language_is_albanian()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("sq");

        var response = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("no-location-sq@example.com", "password123", "Test", "Worker", ""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblem>();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("Vërtetimi dështoi");
        problem.Errors["Location"][0].ShouldBe("'Vendndodhja' nuk mund të jetë bosh.");
    }

    [Fact]
    public async Task Domain_errors_are_translated_when_accept_language_is_albanian()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("sq");

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new Workit.Core.Users.LoginUser.Request("missing@example.com", "password123"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<DetailProblem>();
        problem.ShouldNotBeNull();
        problem.Title.ShouldBe("Gabim biznesi");
        problem.Detail.ShouldBe("Email ose fjalëkalim i pasaktë.");
    }

    [Fact]
    public async Task Job_opening_content_and_enum_labels_follow_accept_language()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var business = await RegisterBusinessAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.AccessToken);

        var workDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));
        var body = new CreateJobOpeningEndpoint.Body(
            "Weekend waiter",
            "Evening dinner service.",
            "Waiter",
            "Tirana",
            8m,
            PayType.Hourly,
            JobType.ShortTerm,
            workDate,
            workDate,
            ShiftType.CustomHours,
            new TimeOnly(16, 0),
            new TimeOnly(22, 0),
            2,
            "en",
            new Dictionary<string, JobOpeningTranslation>
            {
                ["sq"] = new("Kamarier fundjave", "Shërbim darke në mbrëmje.", "Kamarier"),
            });

        var createResponse = await client.PostAsJsonAsync("/job-openings", body);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJobOpening.Response>();
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());
        created.ShouldNotBeNull();
        created.JobTypeLabel.ShouldBe("Short-term");

        var worker = await RegisterWorkerAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", worker.AccessToken);

        var english = await GetJobOpeningAsync(client, created.Id, language: null);
        english.Title.ShouldBe("Weekend waiter");
        english.Role.ShouldBe("Waiter");
        english.Language.ShouldBe("en");
        english.JobTypeLabel.ShouldBe("Short-term");
        english.ShiftTypeLabel.ShouldBe("Custom hours");

        var albanian = await GetJobOpeningAsync(client, created.Id, language: "sq");
        albanian.Title.ShouldBe("Kamarier fundjave");
        albanian.Role.ShouldBe("Kamarier");
        albanian.Language.ShouldBe("sq");
        albanian.JobTypeLabel.ShouldBe("Afatshkurtër");
        albanian.ShiftTypeLabel.ShouldBe("Orar i personalizuar");
    }

    private static async Task<GetJobOpening.Response> GetJobOpeningAsync(HttpClient client, Guid id, string? language)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/job-openings/{id}");
        if (language is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(language);
        }

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<GetJobOpening.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static async Task<RegisterBusiness.Response> RegisterBusinessAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "business@example.com",
                "password123",
                "Test Business",
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterBusiness.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static async Task<RegisterWorker.Response> RegisterWorkerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("worker@example.com", "password123", "Test", "Worker", "Tirana"));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"workit-localization-tests-{Guid.NewGuid()}";
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
                });
            });
    }

    private sealed record ValidationProblem(string Title, Dictionary<string, string[]> Errors);

    private sealed record DetailProblem(string Title, string Detail);
}
