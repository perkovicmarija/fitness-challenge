using FitnessChallenge.Api.DTO;
using FitnessChallenge.Api.Data;
using FitnessChallenge.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessChallenge.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController(
    StandingsQuery standings,
    ChallengeQuery challenges,
    FitnessDbContext db,
    TimeProvider clock) : ControllerBase
{
    private static readonly TimeSpan TrendWindow = TimeSpan.FromDays(7);

    private static readonly TimeSpan WidestClientOffset = TimeSpan.FromDays(2);

    [HttpGet]
    public async Task<ActionResult<LeaderboardResponse>> Get(CancellationToken cancellationToken)
    {
        var current = await standings.AsOfAsync(null, cancellationToken);
        var lastWeek = await standings.AsOfAsync(clock.GetUtcNow().UtcDateTime - TrendWindow, cancellationToken);

        var rankLastWeek = lastWeek
            .Select((standing, index) => (standing.UserId, Rank: index + 1))
            .ToDictionary(entry => entry.UserId, entry => entry.Rank);

        var entries = current
            .Select((standing, index) =>
            {
                var rank = index + 1;
                int? previousRank = rankLastWeek.TryGetValue(standing.UserId, out var found) ? found : null;

                return new LeaderboardEntryResponse(
                    rank,
                    standing.UserId,
                    standing.FirstName,
                    standing.LastName,
                    standing.TotalPoints,
                    previousRank - rank,
                    index == 0 ? null : RankGap.ToOvertake(current[index - 1].TotalPoints, standing.TotalPoints));
            })
            .ToList();

        return new LeaderboardResponse(
            await challenges.CurrentAsync(cancellationToken),
            await ActiveTodayAsync(cancellationToken),
            entries);
    }

    private async Task<int> ActiveTodayAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        var recent = await db.Activities
            .Where(a => a.OccurredAtUtc >= now - WidestClientOffset)
            .Select(a => new { a.UserId, a.LocalDate, a.UtcOffsetMinutes })
            .ToListAsync(cancellationToken);

        return recent
            .Where(a => a.LocalDate == DateOnly.FromDateTime(now.AddMinutes(a.UtcOffsetMinutes)))
            .Select(a => a.UserId)
            .Distinct()
            .Count();
    }
}
