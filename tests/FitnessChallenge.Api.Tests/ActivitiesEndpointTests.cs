using System.Net;
using System.Net.Http.Json;
using System.Text;
using FitnessChallenge.Api.DTO;

namespace FitnessChallenge.Api.Tests;

public class ActivitiesEndpointTests
{
    [Fact]
    public async Task LoggingAnActivityStoresThePointsItEarned()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();
        var userId = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime = "2026-06-30T10:30:00Z",
            sport = "running",
            distance = 42.195m,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var activity = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.Equal(4219, activity!.Points);
    }

    [Fact]
    public async Task TheAssignmentsOwnInvalidExampleIsRejected()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();
        var userId = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime = "2026-06-30T10:30:00Z",
            sport = "swimming",
            distance = 42.195m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TheLocalDateComesFromTheSubmittedOffsetNotFromUtc()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();
        var userId = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime = "2026-07-01T01:30:00+02:00",
            sport = "running",
            distance = 5m,
        });

        var activity = await response.Content.ReadFromJsonAsync<ActivityResponse>();

        Assert.Equal(new DateOnly(2026, 7, 1), activity!.LocalDate);
        Assert.Equal(new DateTime(2026, 6, 30, 23, 30, 0, DateTimeKind.Utc), activity.OccurredAt.UtcDateTime);
    }

    [Fact]
    public async Task ATimestampWithoutAnOffsetIsRejected()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();
        var userId = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime = "2026-06-30T10:30:00",
            sport = "running",
            distance = 5m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnActivityForAnUnknownUserIsRejected()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId = Guid.NewGuid(),
            datetime = "2026-06-30T10:30:00Z",
            sport = "running",
            distance = 5m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnUnknownSportIsRejected()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();
        var userId = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime = "2026-06-30T10:30:00Z",
            sport = "curling",
            distance = 5m,
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DailyStepsAreSubmittedWithNoSportAtAll()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();
        var userId = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime = "2026-06-30T10:30:00Z",
            steps = 399,
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var activity = await response.Content.ReadFromJsonAsync<ActivityResponse>();
        Assert.Equal(3, activity!.Points);
    }

    [Fact]
    public async Task ABodyCarryingAnUnknownFieldIsRejected()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();
        var userId = await RegisterAsync(client);

        var body = $$"""
            {
              "userId": "{{userId}}",
              "datetime": "2026-06-30T10:30:00Z",
              "sport": "running",
              "distance": 5,
              "distnace": 42
            }
            """;

        var response = await client.PostAsync(
            "/api/activities",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<Guid> RegisterAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/users", new { firstName = "Anna", lastName = "Wilson" });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<RegisteredUserResponse>();
        return created!.Id;
    }
}
