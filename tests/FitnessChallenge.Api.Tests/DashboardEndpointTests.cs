using System.Net;
using System.Net.Http.Json;
using FitnessChallenge.Api.DTO;

namespace FitnessChallenge.Api.Tests;

public class DashboardEndpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AnUnknownUserInThePathIsNotFound()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/users/{Guid.NewGuid()}/dashboard");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GroupsPointsByTheDayTheUserExperienced()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();
        var anna = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(anna, "2026-09-06T09:00:00Z", 1m);
        await client.RunAsync(anna, "2026-09-06T18:00:00Z", 2m);
        await client.RunAsync(anna, "2026-09-07T09:00:00Z", 1m);

        var dashboard = (await client.GetFromJsonAsync<DashboardResponse>($"/api/users/{anna}/dashboard"))!;

        Assert.Equal(400, dashboard.TotalPoints);
        Assert.Equal(2, dashboard.PerDay.Count);
        Assert.Equal(new DateOnly(2026, 9, 6), dashboard.PerDay[0].Date);
        Assert.Equal(300, dashboard.PerDay[0].Points);
    }

    [Fact]
    public async Task ReportsEachSportsShareOfTheUsersPoints()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();
        var anna = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(anna, "2026-09-07T09:00:00Z", 3m);
        await client.StepsAsync(anna, "2026-09-07T20:00:00Z", 10_000);

        var dashboard = (await client.GetFromJsonAsync<DashboardResponse>($"/api/users/{anna}/dashboard"))!;

        Assert.Equal(0.75, dashboard.PerSport.Single(s => s.Sport == "running").Share);
        Assert.Equal(0.25, dashboard.PerSport.Single(s => s.Sport is null).Share);
    }

    [Fact]
    public async Task ReportsTheFieldsDistributionAlongsideTheUsersOwn()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();
        var anna = await client.RegisterAsync("Anna", "Wilson");
        var emma = await client.RegisterAsync("Emma", "Davis");

        await client.RunAsync(anna, "2026-09-07T09:00:00Z", 1m);
        await client.StepsAsync(emma, "2026-09-07T09:00:00Z", 10_000);

        var dashboard = (await client.GetFromJsonAsync<DashboardResponse>($"/api/users/{anna}/dashboard"))!;

        Assert.Equal(1.0, dashboard.PerSport.Single(s => s.Sport == "running").Share);
        Assert.Equal(0.5, dashboard.FieldAverage.Single(s => s.Sport == "running").Share);
    }

    [Fact]
    public async Task ADayStillInProgressDoesNotEndTheStreak()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();
        var anna = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(anna, "2026-09-06T09:00:00Z", 1m);
        await client.RunAsync(anna, "2026-09-07T09:00:00Z", 1m);

        var dashboard = (await client.GetFromJsonAsync<DashboardResponse>($"/api/users/{anna}/dashboard"))!;

        Assert.Equal(2, dashboard.CurrentStreakDays);
    }

    [Fact]
    public async Task AMissedDayEndsTheStreak()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();
        var anna = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(anna, "2026-09-04T09:00:00Z", 1m);

        await client.RunAsync(anna, "2026-09-06T09:00:00Z", 1m);
        await client.RunAsync(anna, "2026-09-07T09:00:00Z", 1m);

        var dashboard = (await client.GetFromJsonAsync<DashboardResponse>($"/api/users/{anna}/dashboard"))!;

        Assert.Equal(2, dashboard.CurrentStreakDays);
    }

    [Fact]
    public async Task NarrowsToTheRequestedDates()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();
        var anna = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(anna, "2026-09-01T09:00:00Z", 1m);
        await client.RunAsync(anna, "2026-09-07T09:00:00Z", 2m);

        var dashboard = (await client.GetFromJsonAsync<DashboardResponse>(
            $"/api/users/{anna}/dashboard?from=2026-09-05&to=2026-09-08"))!;

        Assert.Equal(200, dashboard.TotalPoints);
        Assert.Single(dashboard.PerDay);
    }

    [Fact]
    public async Task ListsActivityHistoryNewestFirst()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();
        var anna = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(anna, "2026-09-06T09:00:00Z", 1m);
        await client.RunAsync(anna, "2026-09-07T09:00:00Z", 2m);

        var history = (await client.GetFromJsonAsync<List<ActivityResponse>>($"/api/users/{anna}/activities"))!;

        Assert.Equal([200, 100], history.Select(a => a.Points));
    }
}
