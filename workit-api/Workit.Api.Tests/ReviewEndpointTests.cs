using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Api.Hiring;
using Workit.Api.JobOpenings;
using Workit.Api.Reviews;
using Workit.Api.Tests.TestDoubles;
using Workit.Core.Businesses;
using Workit.Core.Businesses.Domain;
using Workit.Core.Hiring;
using Workit.Core.JobOpenings;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Reviews;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Location;
using Workit.Core.Shared.Persistence;
using Workit.Core.Workers;

namespace Workit.Api.Tests;

public sealed class ReviewEndpointTests
{
    [Fact]
    public async Task Both_sides_can_review_each_other_after_the_assignment_is_completed()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var business = await RegisterBusinessAsync(client);
        var worker = await RegisterWorkerAsync(client);
        var workerProfileId = await GetWorkerProfileIdAsync(client, worker.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.AccessToken);
        var jobOpeningId = await CreateJobOpeningAsync(client, requiredWorkersCount: 1);
        var assignmentId = await HireAsync(client, jobOpeningId, workerProfileId);

        var completeResponse = await client.PostAsync($"/assignments/{assignmentId}/complete", null);
        completeResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await completeResponse.Content.ReadAsStringAsync());

        var businessReviewResponse = await client.PostAsJsonAsync(
            "/reviews",
            new CreateReviewEndpoint.Body(assignmentId, 4, "Reliable and on time."));
        businessReviewResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await businessReviewResponse.Content.ReadAsStringAsync());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", worker.AccessToken);
        var workerReviewResponse = await client.PostAsJsonAsync(
            "/reviews",
            new CreateReviewEndpoint.Body(assignmentId, 5, "Great place to work."));
        workerReviewResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await workerReviewResponse.Content.ReadAsStringAsync());

        var businessProfileId = await GetBusinessProfileIdAsync(factory, business.User.Id);
        var workerReviewsResponse = await client.GetAsync($"/workers/{workerProfileId}/reviews");
        var workerReviews = await workerReviewsResponse.Content.ReadFromJsonAsync<GetWorkerReviews.Response>();
        workerReviewsResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await workerReviewsResponse.Content.ReadAsStringAsync());
        workerReviews.ShouldNotBeNull();
        workerReviews.Items.ShouldHaveSingleItem();
        workerReviews.Items[0].Rating.ShouldBe(4);
        workerReviews.AverageRating.ShouldBe(4);

        var businessReviewsResponse = await client.GetAsync($"/businesses/{businessProfileId}/reviews");
        var businessReviews = await businessReviewsResponse.Content.ReadFromJsonAsync<GetBusinessReviews.Response>();
        businessReviewsResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await businessReviewsResponse.Content.ReadAsStringAsync());
        businessReviews.ShouldNotBeNull();
        businessReviews.Items.ShouldHaveSingleItem();
        businessReviews.Items[0].Rating.ShouldBe(5);
    }

    [Fact]
    public async Task Cannot_review_before_the_assignment_is_completed()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var business = await RegisterBusinessAsync(client);
        var worker = await RegisterWorkerAsync(client);
        var workerProfileId = await GetWorkerProfileIdAsync(client, worker.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.AccessToken);
        var jobOpeningId = await CreateJobOpeningAsync(client, requiredWorkersCount: 1);
        var assignmentId = await HireAsync(client, jobOpeningId, workerProfileId);

        var reviewResponse = await client.PostAsJsonAsync(
            "/reviews",
            new CreateReviewEndpoint.Body(assignmentId, 5, null));
        var errorBody = await reviewResponse.Content.ReadAsStringAsync();

        reviewResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest, errorBody);
        errorBody.ShouldContain("completed");
    }

    [Fact]
    public async Task Cannot_review_the_same_assignment_twice()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var business = await RegisterBusinessAsync(client);
        var worker = await RegisterWorkerAsync(client);
        var workerProfileId = await GetWorkerProfileIdAsync(client, worker.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.AccessToken);
        var jobOpeningId = await CreateJobOpeningAsync(client, requiredWorkersCount: 1);
        var assignmentId = await HireAsync(client, jobOpeningId, workerProfileId);
        await client.PostAsync($"/assignments/{assignmentId}/complete", null);

        var firstResponse = await client.PostAsJsonAsync("/reviews", new CreateReviewEndpoint.Body(assignmentId, 3, null));
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await firstResponse.Content.ReadAsStringAsync());

        var secondResponse = await client.PostAsJsonAsync("/reviews", new CreateReviewEndpoint.Body(assignmentId, 5, null));

        secondResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_worker_who_was_never_assigned_cannot_review_the_assignment()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var business = await RegisterBusinessAsync(client);
        var hiredWorker = await RegisterWorkerAsync(client, "hired@example.com");
        var hiredWorkerProfileId = await GetWorkerProfileIdAsync(client, hiredWorker.AccessToken);
        var outsideWorker = await RegisterWorkerAsync(client, "outsider@example.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.AccessToken);
        var jobOpeningId = await CreateJobOpeningAsync(client, requiredWorkersCount: 1);
        var assignmentId = await HireAsync(client, jobOpeningId, hiredWorkerProfileId);
        await client.PostAsync($"/assignments/{assignmentId}/complete", null);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsideWorker.AccessToken);
        var reviewResponse = await client.PostAsJsonAsync("/reviews", new CreateReviewEndpoint.Body(assignmentId, 5, null));

        reviewResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cannot_hire_more_workers_than_the_job_opening_needs()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var business = await RegisterBusinessAsync(client);
        var firstWorker = await RegisterWorkerAsync(client, "first@example.com");
        var firstWorkerProfileId = await GetWorkerProfileIdAsync(client, firstWorker.AccessToken);
        var secondWorker = await RegisterWorkerAsync(client, "second@example.com");
        var secondWorkerProfileId = await GetWorkerProfileIdAsync(client, secondWorker.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.AccessToken);
        var jobOpeningId = await CreateJobOpeningAsync(client, requiredWorkersCount: 1);
        await HireAsync(client, jobOpeningId, firstWorkerProfileId);

        var secondHireResponse = await client.PostAsJsonAsync(
            $"/job-openings/{jobOpeningId}/assignments",
            new HireWorkerEndpoint.Body(secondWorkerProfileId));

        secondHireResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Top_workers_rank_experienced_well_rated_workers_first()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var business = await RegisterBusinessAsync(client);
        var experienced = await RegisterWorkerAsync(client, "experienced@example.com");
        var newcomer = await RegisterWorkerAsync(client, "newcomer@example.com");
        var experiencedProfileId = await GetWorkerProfileIdAsync(client, experienced.AccessToken);
        var newcomerProfileId = await GetWorkerProfileIdAsync(client, newcomer.AccessToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", business.AccessToken);
        var pastJobId = await CreateJobOpeningAsync(client, requiredWorkersCount: 1);
        var assignmentId = await HireAsync(client, pastJobId, experiencedProfileId);
        (await client.PostAsync($"/assignments/{assignmentId}/complete", null)).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/reviews", new CreateReviewEndpoint.Body(assignmentId, 5, null))).EnsureSuccessStatusCode();

        var newJobId = await CreateJobOpeningAsync(client, requiredWorkersCount: 1);
        var response = await client.GetAsync($"/job-openings/{newJobId}/top-workers");
        var topWorkers = await response.Content.ReadFromJsonAsync<GetTopWorkers.Response>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        topWorkers.ShouldNotBeNull();
        topWorkers.Items.Select(item => item.WorkerProfileId).ShouldBe([experiencedProfileId, newcomerProfileId]);
        topWorkers.Items[0].CompletedSameRoleJobsCount.ShouldBe(1);
        topWorkers.Items[0].AverageRating.ShouldBe(5);
        topWorkers.Items[1].AverageRating.ShouldBeNull();
    }

    private static async Task<Guid> CreateJobOpeningAsync(HttpClient client, int requiredWorkersCount)
    {
        var workDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
        var response = await client.PostAsJsonAsync(
            "/job-openings",
            new CreateJobOpeningEndpoint.Body(
                "Dinner shift waiter",
                "Serve guests during the restaurant dinner service.",
                "Waiter",
                "Tirana",
                8m,
                PayType.Hourly,
                JobType.ShortTerm,
                workDate,
                workDate,
                ShiftType.Evening,
                null,
                null,
                requiredWorkersCount));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var created = await response.Content.ReadFromJsonAsync<CreateJobOpening.Response>();
        created.ShouldNotBeNull();
        return created.Id;
    }

    private static async Task<Guid> HireAsync(HttpClient client, Guid jobOpeningId, Guid workerProfileId)
    {
        var response = await client.PostAsJsonAsync(
            $"/job-openings/{jobOpeningId}/assignments",
            new HireWorkerEndpoint.Body(workerProfileId));
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var created = await response.Content.ReadFromJsonAsync<HireWorker.Response>();
        created.ShouldNotBeNull();
        return created.Id;
    }

    private static async Task<Guid> GetWorkerProfileIdAsync(HttpClient client, string workerAccessToken)
    {
        var originalAuth = client.DefaultRequestHeaders.Authorization;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", workerAccessToken);

        var response = await client.GetAsync("/worker-profile");
        var profile = await response.Content.ReadFromJsonAsync<Workit.Core.Workers.GetWorkerProfile.Response>();
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        profile.ShouldNotBeNull();

        client.DefaultRequestHeaders.Authorization = originalAuth;
        return profile.Id;
    }

    private static async Task<Guid> GetBusinessProfileIdAsync(WebApplicationFactory<Program> factory, Guid businessUserId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Set<BusinessProfile>()
            .Where(profile => profile.UserId == businessUserId)
            .Select(profile => profile.Id)
            .SingleAsync();
    }

    private static async Task<RegisterBusiness.Response> RegisterBusinessAsync(
        HttpClient client,
        string email = "business@example.com",
        string nipt = "K12345678A")
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                email,
                "password123",
                "Test Business",
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m,
                nipt));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterBusiness.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static async Task<RegisterWorker.Response> RegisterWorkerAsync(
        HttpClient client,
        string email = "worker@example.com")
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request(email, "password123", "Test", "Worker", "Tirana"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"workit-review-tests-{Guid.NewGuid()}";
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
                    services.AddSingleton<ICityLookupService>(new FakeCityLookupService());
                    services.RemoveAll<IIdentityVerificationProvider>();
                    services.AddSingleton<IIdentityVerificationProvider, FakeIdentityVerificationProvider>();
                });
            });
    }
}
