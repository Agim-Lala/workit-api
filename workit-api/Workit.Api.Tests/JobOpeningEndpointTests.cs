using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Workit.Api.JobOpenings;
using Workit.Api.Tests.TestDoubles;
using Workit.Api.Workers;
using Workit.Core.Businesses;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Location;
using Workit.Core.Shared.Persistence;
using Workit.Core.Workers;

namespace Workit.Api.Tests;

public sealed class JobOpeningEndpointTests
{
    [Fact]
    public async Task Create_then_get_job_opening_returns_api_data()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var authPayload = await RegisterBusinessAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var businessProfileId = await db.Set<BusinessProfile>()
            .Where(profile => profile.UserId == authPayload.User.Id)
            .Select(profile => profile.Id)
            .SingleAsync();
        var workDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var createRequest = new CreateJobOpeningEndpoint.Body(
            "Event assistant",
            "Help run check-in and guest coordination for an evening event.",
            "Event Staff",
            "Tirana",
            8m,
            PayType.Hourly,
            JobType.ShortTerm,
            workDate,
            workDate,
            ShiftType.CustomHours,
            new TimeOnly(16, 0),
            new TimeOnly(22, 0),
            3);

        var createResponse = await client.PostAsJsonAsync("/job-openings", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateJobOpening.Response>();

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());
        created.ShouldNotBeNull();

        var workerAuthPayload = await RegisterWorkerAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", workerAuthPayload.AccessToken);

        var getResponse = await client.GetAsync($"/job-openings/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<GetJobOpening.Response>();

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK, await getResponse.Content.ReadAsStringAsync());
        fetched.ShouldNotBeNull();
        fetched.Id.ShouldBe(created.Id);
        fetched.BusinessProfileId.ShouldBe(businessProfileId);
        fetched.Title.ShouldBe(createRequest.Title);
        fetched.Role.ShouldBe(createRequest.Role);
        fetched.PayType.ShouldBe(PayType.Hourly);
        fetched.JobType.ShouldBe(JobType.ShortTerm);
        fetched.StartDate.ShouldBe(workDate);
        fetched.EndDate.ShouldBe(workDate);
        fetched.ShiftType.ShouldBe(ShiftType.CustomHours);
        fetched.ShiftStartTime.ShouldBe(new TimeOnly(16, 0));
        fetched.ShiftEndTime.ShouldBe(new TimeOnly(22, 0));
        fetched.RequiredWorkersCount.ShouldBe(3);
    }

    [Fact]
    public async Task Browse_filters_jobs_by_type_date_and_shift()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var authPayload = await RegisterBusinessAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var workDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3));

        var shortTermResponse = await client.PostAsJsonAsync(
            "/job-openings",
            new CreateJobOpeningEndpoint.Body(
                "Dinner shift waiter",
                "Serve guests during the restaurant dinner service.",
                "Waiter",
                "Tirana",
                7m,
                PayType.Hourly,
                JobType.ShortTerm,
                workDate,
                workDate,
                ShiftType.CustomHours,
                new TimeOnly(16, 0),
                new TimeOnly(22, 0),
                1));
        shortTermResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await shortTermResponse.Content.ReadAsStringAsync());

        var permanentResponse = await client.PostAsJsonAsync(
            "/job-openings",
            new CreateJobOpeningEndpoint.Body(
                "Permanent breakfast waiter",
                "Join the restaurant's permanent breakfast service team.",
                "Waiter",
                "Tirana",
                900m,
                PayType.Monthly,
                JobType.Permanent,
                workDate,
                null,
                ShiftType.Morning,
                null,
                null,
                2));
        permanentResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await permanentResponse.Content.ReadAsStringAsync());

        var projectResponse = await client.PostAsJsonAsync(
            "/job-openings",
            new CreateJobOpeningEndpoint.Body(
                "Restaurant renovation project",
                "Work on a two-week restaurant renovation.",
                "General Worker",
                "Tirana",
                70m,
                PayType.Daily,
                JobType.Project,
                workDate,
                workDate.AddDays(13),
                ShiftType.Evening,
                null,
                null,
                4));
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateJobOpening.Response>();

        projectResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await projectResponse.Content.ReadAsStringAsync());
        project.ShouldNotBeNull();
        project.JobType.ShouldBe(JobType.Project);
        project.EndDate.ShouldBe(workDate.AddDays(13));
        project.ShiftType.ShouldBe(ShiftType.Evening);

        var workerAuthPayload = await RegisterWorkerAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            workerAuthPayload.AccessToken);

        var response = await client.GetAsync(
            $"/job-openings?jobType=ShortTerm&onDate={workDate:yyyy-MM-dd}&shiftType=CustomHours");
        var payload = await response.Content.ReadFromJsonAsync<GetJobOpenings.Response>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        payload.ShouldNotBeNull();
        payload.TotalCount.ShouldBe(1);
        payload.Items.Single().Title.ShouldBe("Dinner shift waiter");
        payload.Items.Single().ShiftStartTime.ShouldBe(new TimeOnly(16, 0));
        payload.Items.Single().ShiftEndTime.ShouldBe(new TimeOnly(22, 0));
    }

    [Fact]
    public async Task Browse_uses_the_worker_location_and_updates_when_the_profile_changes()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var business = await RegisterBusinessAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            business.AccessToken);
        var workDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var tiranaResponse = await client.PostAsJsonAsync(
            "/job-openings",
            CreateJobOpeningBody("Tirana event host", workDate));
        tiranaResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await tiranaResponse.Content.ReadAsStringAsync());

        var durresResponse = await client.PostAsJsonAsync(
            "/job-openings",
            new CreateJobOpeningEndpoint.Body(
                "Durrës event host",
                "Welcome guests during a short-term event.",
                "Event Host",
                "Beachfront, Durrës",
                8m,
                PayType.Hourly,
                JobType.ShortTerm,
                workDate,
                workDate,
                ShiftType.Evening,
                null,
                null,
                1));
        durresResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await durresResponse.Content.ReadAsStringAsync());

        var worker = await RegisterWorkerAsync(client, "tirana");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            worker.AccessToken);

        var tiranaJobsResponse = await client.GetAsync("/job-openings");
        var tiranaJobs = await tiranaJobsResponse.Content.ReadFromJsonAsync<GetJobOpenings.Response>();

        tiranaJobsResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await tiranaJobsResponse.Content.ReadAsStringAsync());
        tiranaJobs.ShouldNotBeNull();
        tiranaJobs.Items.ShouldHaveSingleItem();
        tiranaJobs.Items[0].Title.ShouldBe("Tirana event host");

        var updateResponse = await client.PutAsJsonAsync(
            "/worker-profile/location",
            new UpdateWorkerLocationEndpoint.Body("Durrës"));
        updateResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await updateResponse.Content.ReadAsStringAsync());

        var profileResponse = await client.GetAsync("/worker-profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<GetWorkerProfile.Response>();
        profileResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await profileResponse.Content.ReadAsStringAsync());
        profile.ShouldNotBeNull();
        profile.Location.ShouldBe("Durrës");

        var durresJobsResponse = await client.GetAsync("/job-openings");
        var durresJobs = await durresJobsResponse.Content.ReadFromJsonAsync<GetJobOpenings.Response>();

        durresJobsResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await durresJobsResponse.Content.ReadAsStringAsync());
        durresJobs.ShouldNotBeNull();
        durresJobs.Items.ShouldHaveSingleItem();
        durresJobs.Items[0].Title.ShouldBe("Durrës event host");
    }

    [Fact]
    public async Task Business_browses_only_its_own_job_openings()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var firstBusiness = await RegisterBusinessAsync(
            client,
            "first-business@example.com",
            "First Business");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            firstBusiness.AccessToken);
        var workDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));

        var firstCreateResponse = await client.PostAsJsonAsync(
            "/job-openings",
            CreateJobOpeningBody("First business waiter", workDate));
        firstCreateResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await firstCreateResponse.Content.ReadAsStringAsync());

        var secondBusiness = await RegisterBusinessAsync(
            client,
            "second-business@example.com",
            "Second Business");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            secondBusiness.AccessToken);

        var secondCreateResponse = await client.PostAsJsonAsync(
            "/job-openings",
            CreateJobOpeningBody("Second business waiter", workDate));
        secondCreateResponse.StatusCode.ShouldBe(
            HttpStatusCode.Created,
            await secondCreateResponse.Content.ReadAsStringAsync());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            firstBusiness.AccessToken);

        var response = await client.GetAsync("/business/job-openings?page=1&pageSize=50");
        var payload = await response.Content.ReadFromJsonAsync<GetBusinessJobOpenings.Response>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        payload.ShouldNotBeNull();
        payload.TotalCount.ShouldBe(1);
        payload.Items.Single().Title.ShouldBe("First business waiter");
    }

    [Fact]
    public async Task Project_without_end_date_is_rejected()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var authPayload = await RegisterBusinessAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var response = await client.PostAsJsonAsync(
            "/job-openings",
            new CreateJobOpeningEndpoint.Body(
                "Renovation project",
                "Help complete a restaurant renovation project.",
                "General Worker",
                "Tirana",
                70m,
                PayType.Daily,
                JobType.Project,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                null,
                ShiftType.Morning,
                null,
                null,
                3));
        var errorBody = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, errorBody);
        errorBody.ShouldContain(nameof(CreateJobOpening.Request.EndDate));
        errorBody.ShouldContain("require an end date");
    }

    [Fact]
    public async Task Worker_cannot_create_job_opening()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var workerAuthPayload = await RegisterWorkerAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", workerAuthPayload.AccessToken);

        var createRequest = new CreateJobOpeningEndpoint.Body(
            "Event assistant",
            "Help run check-in and guest coordination for an evening event.",
            "Event Staff",
            "Tirana",
            8m,
            PayType.Hourly,
            JobType.ShortTerm,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            ShiftType.CustomHours,
            new TimeOnly(16, 0),
            new TimeOnly(22, 0),
            3);

        var response = await client.PostAsJsonAsync("/job-openings", createRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Business_cannot_browse_worker_job_openings_api()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var authPayload = await RegisterBusinessAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        var response = await client.GetAsync("/job-openings");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Worker_cannot_browse_business_job_openings_api()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var authPayload = await RegisterWorkerAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            authPayload.AccessToken);

        var response = await client.GetAsync("/business/job-openings");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<RegisterBusiness.Response> RegisterBusinessAsync(
        HttpClient client,
        string email = "business@example.com",
        string businessName = "Test Business")
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register/business",
            new RegisterBusiness.Request(
                email,
                "password123",
                businessName,
                "Rruga Test, Tirane",
                41.3275m,
                19.8189m));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterBusiness.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static CreateJobOpeningEndpoint.Body CreateJobOpeningBody(
        string title,
        DateOnly workDate)
    {
        return new CreateJobOpeningEndpoint.Body(
            title,
            "Serve guests during the restaurant dinner service.",
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
            1);
    }

    private static async Task<RegisterWorker.Response> RegisterWorkerAsync(
        HttpClient client,
        string location = "Tirana")
    {
        var response = await client.PostAsJsonAsync(
            "/auth/register/worker",
            new RegisterWorker.Request(
                "worker@example.com",
                "password123",
                "Test",
                "Worker",
                location));

        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var payload = await response.Content.ReadFromJsonAsync<RegisterWorker.Response>();
        payload.ShouldNotBeNull();
        return payload;
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"workit-job-opening-tests-{Guid.NewGuid()}";
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
                    services.AddSingleton<ICityLookupService>(new FakeCityLookupService());
                    services.RemoveAll<IIdentityVerificationProvider>();
                    services.AddSingleton<IIdentityVerificationProvider, FakeIdentityVerificationProvider>();
                });
            });
    }
}
