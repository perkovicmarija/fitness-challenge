using System.Net.Http.Json;
using FitnessChallenge.Api.DTO;

namespace FitnessChallenge.Api.Tests;

public class SportsEndpointTests
{
    [Fact]
    public async Task ServesEveryScoredActivityWithItsRate()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        var sports = (await client.GetFromJsonAsync<List<SportResponse>>("/api/sports"))!;

        Assert.Equal(6, sports.Count);

        var running = sports.Single(s => s.Sport == "running");
        Assert.Equal("distance", running.Metric);
        Assert.Equal("km", running.Unit);
        Assert.Equal(100m, running.PointsPerUnit);
    }

    [Fact]
    public async Task DailyStepsIsServedWithNoSportValue()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        var sports = (await client.GetFromJsonAsync<List<SportResponse>>("/api/sports"))!;

        var steps = sports.Single(s => s.Sport is null);
        Assert.Equal("Daily steps", steps.Label);
        Assert.Equal("count", steps.Metric);
    }
}
