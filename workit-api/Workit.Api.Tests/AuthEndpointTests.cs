using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Core.Businesses;
using Workit.Core.Businesses.Domain;
using Workit.Core.Shared.Persistence;
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
            "Worker");

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
            19.8189m);

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
    }

    [Fact]
    public async Task Register_returns_bad_request_when_email_already_exists()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var firstResponse = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("user@example.com", "password123", "Test", "Worker"));
        var secondResponse = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                "user@example.com",
                "password123",
                "Test Business",
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m));

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
    public async Task Get_users_returns_paginated_users()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request("user@example.com", "password123", "Test", "Worker"));
        var authPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        authPayload.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

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

    private static WebApplicationFactory<Program> CreateFactory()
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
                });
            });
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
