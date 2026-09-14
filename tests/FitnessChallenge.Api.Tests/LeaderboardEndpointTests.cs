using System.Globalization;
using System.Net.Http.Json;
using FitnessChallenge.Api.DTO;
using FitnessChallenge.Api.Data;
using Microsoft.Extensions.DependencyInjection;

namespace FitnessChallenge.Api.Tests;

public class LeaderboardEndpointTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RanksUsersByTotalPoints()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var anna = await client.RegisterAsync("Anna", "Wilson");
        var emma = await client.RegisterAsync("Emma", "Davis");

        await client.RunAsync(anna, "2026-09-07T10:00:00Z", 1m);
        await client.RunAsync(emma, "2026-09-07T10:00:00Z", 5m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!.Entries;

        Assert.Equal([emma, anna], board.Select(entry => entry.UserId));
        Assert.Equal([1, 2], board.Select(entry => entry.Rank));
        Assert.Equal(500, board[0].TotalPoints);
    }

    [Fact]
    public async Task BreaksTiesByWhoReachedTheTotalFirst()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var early = await client.RegisterAsync("Anna", "Wilson");
        var late = await client.RegisterAsync("Emma", "Davis");

        await client.RunAsync(early, "2026-09-05T10:00:00Z", 1m);
        await client.RunAsync(late, "2026-09-06T10:00:00Z", 1m);

        var first = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!.Entries;
        var second = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!.Entries;

        Assert.Equal(early, first[0].UserId);
        Assert.Equal(first.Select(e => e.UserId), second.Select(e => e.UserId));
    }

    [Fact]
    public async Task ReportsHowManyPlacesAUserHasMoved()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var anna = await client.RegisterAsync("Anna", "Wilson");
        var emma = await client.RegisterAsync("Emma", "Davis");

        await client.RunAsync(anna, "2026-08-29T10:00:00Z", 1m);
        await client.RunAsync(emma, "2026-09-06T10:00:00Z", 5m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!.Entries;

        var dropped = board.Single(entry => entry.UserId == anna);
        Assert.Equal(2, dropped.Rank);
        Assert.Equal(-1, dropped.RankDelta);
    }

    [Fact]
    public async Task AUserWithNoHistoryAWeekAgoHasNoTrendRatherThanZero()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var newcomer = await client.RegisterAsync("Anna", "Wilson");
        await client.RunAsync(newcomer, "2026-09-07T10:00:00Z", 1m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!.Entries;

        Assert.Null(board![0].RankDelta);
    }

    [Fact]
    public async Task UsersWhoHaveLoggedNothingDoNotAppear()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        await client.RegisterAsync("Anna", "Wilson");

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!.Entries;

        Assert.Empty(board!);
    }

    [Fact]
    public async Task CountsEachPersonWhoTrainedTodayOnce()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var anna = await client.RegisterAsync("Anna", "Wilson");
        var emma = await client.RegisterAsync("Emma", "Davis");

        await client.RunAsync(anna, "2026-09-08T07:00:00Z", 1m);
        await client.RunAsync(anna, "2026-09-08T19:00:00Z", 1m);
        await client.RunAsync(emma, "2026-09-07T10:00:00Z", 1m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!;

        Assert.Equal(1, board.ActiveToday);
    }

    [Fact]
    public async Task ResolvesTodayOnTheSubmittersOwnClock()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var anna = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(anna, "2026-09-09T01:00:00+13:00", 1m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!;

        Assert.Equal(1, board.ActiveToday);
    }

    [Fact]
    public async Task ReportsNobodyActiveWhenTheDayIsStillEmpty()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var anna = await client.RegisterAsync("Anna", "Wilson");
        await client.RunAsync(anna, "2026-09-07T10:00:00Z", 1m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!;

        Assert.Equal(0, board.ActiveToday);
    }

    [Fact]
    public async Task RanksWithoutAChallengeWhenNoneIsConfigured()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var anna = await client.RegisterAsync("Anna", "Wilson");
        await client.RunAsync(anna, "2026-09-07T10:00:00Z", 1m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!;

        Assert.Null(board.Challenge);
        Assert.Single(board.Entries);
    }

    [Theory]

    [InlineData("2026-09-01", "2026-09-30", 8, 30, 22, false)]
    [InlineData("2026-09-08", "2026-09-08", 1, 1, 0, false)]
    [InlineData("2026-08-01", "2026-09-05", 36, 36, 0, true)]
    [InlineData("2026-09-20", "2026-09-29", 0, 10, 21, false)]
    public async Task DescribesTheChallengeAgainstToday(
        string startsOn, string endsOn, int day, int totalDays, int daysLeft, bool hasEnded)
    {
        using var factory = new FitnessApiFactory { Now = Now };
        await AddChallengeAsync(factory, startsOn, endsOn);

        var board = (await factory.CreateClient()
            .GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!;

        var challenge = board.Challenge!;

        Assert.Equal("Move More", challenge.Name);
        Assert.Equal(day, challenge.Day);
        Assert.Equal(totalDays, challenge.TotalDays);
        Assert.Equal(daysLeft, challenge.DaysLeft);
        Assert.Equal(hasEnded, challenge.HasEnded);
    }

    private static async Task AddChallengeAsync(FitnessApiFactory factory, string startsOn, string endsOn)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();

        db.Challenges.Add(new Challenge
        {
            Id = Guid.NewGuid(),
            Name = "Move More",
            StartsOn = DateOnly.Parse(startsOn, CultureInfo.InvariantCulture),
            EndsOn = DateOnly.Parse(endsOn, CultureInfo.InvariantCulture),
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task TheGapToOvertakeIsTheOneTheCoachQuotes()
    {
        using var factory = new FitnessApiFactory { Now = Now };
        var client = factory.CreateClient();

        var ahead = await client.RegisterAsync("Sarah", "Miller");
        var behind = await client.RegisterAsync("Anna", "Wilson");

        await client.RunAsync(ahead, "2026-09-07T10:00:00Z", 5m);
        await client.RunAsync(behind, "2026-09-07T11:00:00Z", 4.27m);

        var board = (await client.GetFromJsonAsync<LeaderboardResponse>("/api/leaderboard"))!;
        var coach = (await client.GetFromJsonAsync<CoachFactsResponse>($"/api/users/{behind}/coach"))!;

        var chaser = board.Entries.Single(entry => entry.UserId == behind);

        Assert.Equal(74, chaser.PointsToOvertake);
        Assert.Equal(coach.PointsToPassNextRank, chaser.PointsToOvertake);
        Assert.Null(board.Entries[0].PointsToOvertake);
    }
}
