using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Core.Shared.Persistence;
using Workit.Core.Users;

namespace Workit.Api.Tests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task Register_then_login_returns_access_tokens()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var request = new RegisterUser.Request("user@example.com", "password123");

        var registerResponse = await client.PostAsJsonAsync("/auth/register", request);
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginUser.Request(request.Email, request.Password));

        registerResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await registerResponse.Content.ReadAsStringAsync());
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await loginResponse.Content.ReadAsStringAsync());

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterUser.Response>();
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginUser.Response>();

        registerPayload.ShouldNotBeNull();
        loginPayload.ShouldNotBeNull();
        registerPayload.User.Email.ShouldBe("user@example.com");
        loginPayload.User.Id.ShouldBe(registerPayload.User.Id);
        loginPayload.AccessToken.ShouldNotBeNullOrWhiteSpace();
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
            "/auth/register",
            new RegisterUser.Request("user@example.com", "password123"));
        var authPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterUser.Response>();
        authPayload.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        var response = await client.GetAsync("/users?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<GetAllUsers.Response>();
        payload.ShouldNotBeNull();
        payload.Items.ShouldHaveSingleItem();
        payload.Items[0].Email.ShouldBe("user@example.com");
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
}
