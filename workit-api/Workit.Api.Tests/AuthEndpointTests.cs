using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Api.Tests.TestDoubles;
using Workit.Core.Businesses;
using Workit.Core.Businesses.Domain;
using Workit.Core.Shared.Email;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Location;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Tokens;
using Workit.Core.Users;
using Workit.Core.Users.Domain;
using Workit.Core.Workers;
using Workit.Core.Workers.Domain;

namespace Workit.Api.Tests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task Register_then_login_returns_access_tokens()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new RegisterWorker.Request(
            "user@example.com",
            "password123",
            "Test",
            "Worker",
            "Tirana");

        var registerResponse = await client.PostAsJsonAsync("/auth/register/worker", request);
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginUser.Request(request.Email, request.Password));

        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await registerResponse.Content.ReadAsStringAsync());
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await loginResponse.Content.ReadAsStringAsync());

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginUser.Response>();

        registerPayload.ShouldNotBeNull();
        loginPayload.ShouldNotBeNull();
        registerPayload.User.Email.ShouldBe("user@example.com");
        registerPayload.User.Role.ShouldBe(UserRole.Worker);
        loginPayload.User.Id.ShouldBe(registerPayload.User.Id);
        loginPayload.User.Role.ShouldBe(UserRole.Worker);
        loginPayload.AccessToken.ShouldNotBeNullOrWhiteSpace();
        GetRoleFromToken(loginPayload.AccessToken).ShouldBe(UserRole.Worker.ToString());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var workerProfile = await db.Set<WorkerProfile>().SingleAsync();
        workerProfile.UserId.ShouldBe(registerPayload.User.Id);
        workerProfile.FirstName.ShouldBe("Test");
        workerProfile.LastName.ShouldBe("Worker");
        workerProfile.Location.ShouldBe("Tirana");
    }

    [Fact]
    public async Task Register_business_returns_business_user()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new RegisterBusiness.Request(
            "business@example.com",
            "password123",
            "Test Business",
            "Rruga Test, Tirane",
            41.3275m,
            19.8189m,
            "K12345678A");

        var response = await client.PostAsJsonAsync("/auth/register/business", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterBusiness.Response>();
        payload.ShouldNotBeNull();
        payload.User.Email.ShouldBe("business@example.com");
        payload.User.Role.ShouldBe(UserRole.Business);
        GetRoleFromToken(payload.AccessToken).ShouldBe(UserRole.Business.ToString());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var businessProfile = await db.Set<BusinessProfile>().SingleAsync();
        businessProfile.UserId.ShouldBe(payload.User.Id);
        businessProfile.BusinessName.ShouldBe("Test Business");
        businessProfile.FullAddress.ShouldBe("Rruga Test, Tirane");
        businessProfile.Nipt.ShouldBe("K12345678A");
        payload.User.EmailConfirmationStatus.ShouldBe(EmailConfirmationStatus.Pending);
    }

    [Fact]
    public async Task Register_business_without_coordinates_geocodes_the_typed_address()
    {
        await using var factory = CreateFactory(new CityLookupResult("Tirana", "Albania", 41.33, 19.82));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "business@example.com",
                "password123",
                "Test Business",
                "Rruga Myslym Shyri, Tirane",
                null,
                null,
                "K12345678A"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var businessProfile = await db.Set<BusinessProfile>().SingleAsync();
        businessProfile.Latitude.ShouldBe(41.33m);
        businessProfile.Longitude.ShouldBe(19.82m);
    }

    [Fact]
    public async Task Register_business_without_coordinates_rejects_an_unknown_address()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "business@example.com",
                "password123",
                "Test Business",
                "Nowhere street 999",
                null,
                null,
                "K12345678A"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Set<BusinessProfile>().AnyAsync()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-nipt")]
    [InlineData("K1234567")]
    [InlineData("123456789A")]
    public async Task Register_business_requires_a_valid_nipt(string nipt)
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "business@example.com",
                "password123",
                "Test Business",
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m,
                nipt));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_business_returns_bad_request_when_nipt_already_registered()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "first-business@example.com",
                "password123",
                "First Business",
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m,
                "K12345678A"));
        var secondResponse = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "second-business@example.com",
                "password123",
                "Second Business",
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m,
                "k12345678a"));

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await firstResponse.Content.ReadAsStringAsync());
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_sends_a_confirmation_email_that_confirms_the_account()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("worker@example.com", "password123", "Test", "Worker", "Tirana"));
        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await registerResponse.Content.ReadAsStringAsync());

        var emailSender = (FakeEmailSender)factory.Services.GetRequiredService<IEmailSender>();
        var sentEmail = (await WaitForSentEmailAsync(emailSender)).ShouldHaveSingleItem();
        sentEmail.ToAddress.ShouldBe("worker@example.com");
        sentEmail.TemplateId.ShouldBe("confirm-email");
        var token = ExtractConfirmationToken(sentEmail.Variables["confirmationLink"]);

        var confirmResponse = await client.PostAsJsonAsync("/auth/confirm-email", new ConfirmEmail.Request(token));

        confirmResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await confirmResponse.Content.ReadAsStringAsync());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Set<User>().SingleAsync();
        user.EmailConfirmed.ShouldBeTrue();
    }

    [Fact]
    public async Task Confirm_email_returns_bad_request_for_an_invalid_token()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/confirm-email", new ConfirmEmail.Request("not-a-real-token"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Resend_confirmation_returns_ok_regardless_of_whether_the_email_is_registered()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/resend-confirmation",
            new ResendEmailConfirmation.Request("unknown@example.com"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_worker_requires_a_location()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request(
                "worker-without-location@example.com",
                "password123",
                "Test",
                "Worker",
                ""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_returns_bad_request_when_email_already_exists()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("user@example.com", "password123", "Test", "Worker", "Tirana"));
        var secondResponse = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "user@example.com",
                "password123",
                "Test Business",
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m,
                "K12345678A"));

        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await firstResponse.Content.ReadAsStringAsync());
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_users_requires_authorization()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_users_rejects_non_admin_users()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("user@example.com", "password123", "Test", "Worker", "Tirana"));
        var authPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        authPayload.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        var response = await client.GetAsync("/users?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_users_returns_paginated_users_for_admin()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("user@example.com", "password123", "Test", "Worker", "Tirana"));
        var authPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        authPayload.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateAccessToken(factory, UserRole.Admin));

        var response = await client.GetAsync("/users?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<GetAllUsers.Response>();
        payload.ShouldNotBeNull();
        payload.Items.ShouldHaveSingleItem();
        payload.Items[0].Email.ShouldBe("user@example.com");
        payload.Items[0].Role.ShouldBe(UserRole.Worker);
        payload.Page.ShouldBe(1);
        payload.PageSize.ShouldBe(10);
        payload.TotalCount.ShouldBe(1);
        payload.TotalPages.ShouldBe(1);
        payload.HasPreviousPage.ShouldBeFalse();
        payload.HasNextPage.ShouldBeFalse();
    }

    private static WebApplicationFactory<Program> CreateFactory(CityLookupResult? cityLookupResult = null)
    {
        var databaseName = $"workit-auth-tests-{Guid.NewGuid()}";
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

                    // Never let tests reach the real Nominatim/Stripe/Resend third parties.
                    services.RemoveAll<ICityLookupService>();
                    services.AddSingleton<ICityLookupService>(new FakeCityLookupService(cityLookupResult));
                    services.RemoveAll<IIdentityVerificationProvider>();
                    services.AddSingleton<IIdentityVerificationProvider, FakeIdentityVerificationProvider>();
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender, FakeEmailSender>();
                });
            });
    }

    private static string CreateAccessToken(WebApplicationFactory<Program> factory, UserRole role)
    {
        using var scope = factory.Services.CreateScope();
        var accessTokenCreator = scope.ServiceProvider.GetRequiredService<IAccessTokenCreator>();
        var user = new User(
            $"{role.ToString().ToLowerInvariant()}@example.com",
            "password-hash",
            DateTimeOffset.UtcNow,
            role);

        return accessTokenCreator.Create(user, DateTimeOffset.UtcNow.AddMinutes(30));
    }

    private static async Task<IReadOnlyList<(string ToAddress, string TemplateId, IReadOnlyDictionary<string, string> Variables)>> WaitForSentEmailAsync(
        FakeEmailSender emailSender,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(2));
        while (emailSender.SentEmails.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        return emailSender.SentEmails;
    }

    private static string ExtractConfirmationToken(string confirmationLink)
    {
        var match = Regex.Match(confirmationLink, "token=([^\"&]+)");
        match.Success.ShouldBeTrue();
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }

    private static string? GetRoleFromToken(string accessToken)
    {
        var payload = DecodeBase64Url(accessToken.Split('.')[1]);
        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            payload);

        if (json is null)
        {
            return null;
        }

        if (json.TryGetValue(ClaimTypes.Role, out var namespacedRole))
        {
            return namespacedRole.GetString();
        }

        return json.TryGetValue("role", out var role)
            ? role.GetString()
            : null;
    }

    private static string DecodeBase64Url(string value)
    {
        var padded = value
            .Replace('-', '+')
            .Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }
}
