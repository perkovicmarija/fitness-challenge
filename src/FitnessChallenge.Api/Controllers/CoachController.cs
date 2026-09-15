using FitnessChallenge.Api.Coach;
using FitnessChallenge.Api.DTO;
using FitnessChallenge.Api.Data;
using FitnessChallenge.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FitnessChallenge.Api.Controllers;

[ApiController]
[Route("api/users/{id:guid}/coach")]
public class CoachController(
    FitnessDbContext db,
    StandingsQuery standings,
    ChallengeQuery challenges,
    ICoachClient coach,
    IOptions<CoachOptions> options,
    TimeProvider clock) : ControllerBase
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromDays(7);

    private static readonly TimeSpan HabitWindow = TimeSpan.FromDays(28);

    private const int ActivitiesBeforeAPattern = 8;

    [HttpGet]
    public async Task<ActionResult<CoachFactsResponse>> GetFacts(Guid id, CancellationToken cancellationToken)
    {
        var facts = await FactsAsync(id, cancellationToken);

        if (facts is null)
        {
            return NotFound();
        }

        return facts;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<CoachChatResponse>> Chat(
        Guid id, CoachChatRequest request, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {

            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The AI coach is not configured.",
                detail: "Set Coach__Endpoint, Coach__ApiKey and Coach__Deployment to switch it on. See .env.example.");
        }

        var facts = await FactsAsync(id, cancellationToken);

        if (facts is null)
        {
            return NotFound();
        }

        try
        {
            var reply = await coach.ReplyAsync(
                CoachPrompt.Build(facts),
                [.. request.Messages.Select(message => new CoachMessage(message.Role, message.Text))],
                cancellationToken);

            return new CoachChatResponse(reply);
        }
        catch (Exception failure)
            when (failure is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {

            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The AI coach could not be reached.",
                detail: failure.Message);
        }
    }

    [HttpGet("insight/{kind}")]
    public async Task<ActionResult<CoachChatResponse>> Insight(
        Guid id, string kind, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<CoachPrompt.Ask>(kind, ignoreCase: true, out var ask) || ask == CoachPrompt.Ask.Advice)
        {
            ModelState.AddModelError(nameof(kind), "kind must be 'rankChange' or 'weeklyRecap'.");
            return ValidationProblem(ModelState);
        }

        if (!options.Value.IsConfigured)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "The AI coach is not configured.",
                detail: "Set Coach__Endpoint, Coach__ApiKey and Coach__Deployment to switch it on. See .env.example.");
        }

        var facts = await FactsAsync(id, cancellationToken);

        if (facts is null)
        {
            return NotFound();
        }

        if (ask == CoachPrompt.Ask.RankChange && facts.RankChange is null)
        {
            return NoContent();
        }

        try
        {
            var reply = await coach.ReplyAsync(
                CoachPrompt.Build(facts, ask),
                [new CoachMessage("user", ask == CoachPrompt.Ask.RankChange
                    ? "Why did my rank change?"
                    : "What happened this week?")],
                cancellationToken);

            return new CoachChatResponse(reply);
        }
        catch (Exception failure)
            when (failure is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "The AI coach could not be reached.",
                detail: failure.Message);
        }
    }

    private async Task<CoachFactsResponse?> FactsAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id, cancellationToken))
        {
            return null;
        }

        var nowUtc = clock.GetUtcNow().UtcDateTime;

        var competitors = await db.Users.CountAsync(cancellationToken);
        var board = await standings.AsOfAsync(null, cancellationToken);
        var position = board.FindIndex(standing => standing.UserId == id);
        var myPoints = position >= 0 ? board[position].TotalPoints : 0;

        Standing? above = position switch
        {
            0 => null,
            > 0 => board[position - 1],

            _ => board.Count > 0 ? board[^1] : null,
        };

        int? toPassNextRank = above is null ? null : RankGap.ToOvertake(above.TotalPoints, myPoints);
        int? toTakeTheLead = position == 0 || board.Count == 0 ? null : RankGap.ToOvertake(board[0].TotalPoints, myPoints);

        int? leadOverNextRank = position == 0 && board.Count > 1 ? myPoints - board[1].TotalPoints : null;

        var since = nowUtc - RecentWindow;
        var recent = db.Activities.Where(a => a.UserId == id && a.OccurredAtUtc >= since);

        var daysTrained = await db.Activities
            .Where(a => a.UserId == id && a.OccurredAtUtc >= nowUtc - HabitWindow)
            .Select(a => a.LocalDate)
            .Distinct()
            .CountAsync(cancellationToken);

        var startedUtc = await db.Activities
            .Where(a => a.UserId == id)
            .Select(a => (DateTime?)a.OccurredAtUtc)
            .MinAsync(cancellationToken);

        var sports = await SportsAsync(id, cancellationToken);

        var ways = PointsGap.WaysToEarn(toPassNextRank ?? 0).Select(Describe).ToList();

        var strongest = sports.FirstOrDefault(sport => sport.Sessions > 0);

        var challenge = await challenges.CurrentAsync(cancellationToken);

        return new CoachFactsResponse(
            id,
            challenge,
            position >= 0 ? position + 1 : null,
            competitors,
            myPoints,
            toPassNextRank,
            toTakeTheLead,
            await RankChangeAsync(id, board, position, cancellationToken),
            Outlook(toPassNextRank, WeeklyAverage(myPoints, startedUtc, nowUtc), challenge),
            await ActiveDaysAsync(id, nowUtc, cancellationToken),
            await BestDayAsync(id, nowUtc, cancellationToken),
            leadOverNextRank,
            above is null ? null : $"{above.FirstName} {above.LastName}",
            await recent.CountAsync(cancellationToken),
            await recent.SumAsync(a => a.Points, cancellationToken),
            WeeklyAverage(myPoints, startedUtc, nowUtc),
            daysTrained,
            await BestWeekdayAsync(id, cancellationToken),
            ways,
            strongest is null ? null : ways.FirstOrDefault(way => way.Label == strongest.Label),
            sports);
    }

    private async Task<List<CoachSportResponse>> SportsAsync(Guid id, CancellationToken cancellationToken)
    {
        var done = await db.Activities
            .Where(a => a.UserId == id)
            .GroupBy(a => a.Sport)
            .Select(g => new { Sport = g.Key, Points = g.Sum(a => a.Points), Sessions = g.Count() })
            .ToDictionaryAsync(row => row.Sport, row => new { row.Points, row.Sessions }, cancellationToken);

        return ActivityRules.All
            .Select(rule =>
            {
                var mine = done.GetValueOrDefault(rule.Sport);
                var sessions = mine?.Sessions ?? 0;

                return new CoachSportResponse(
                    SportWire.Label(rule.Sport),
                    mine?.Points ?? 0,
                    sessions,
                    sessions == 0 ? 0 : (int)Math.Round((double)mine!.Points / sessions));
            })
            .OrderByDescending(sport => sport.Points)
            .ToList();
    }

    private static GapOutlookResponse? Outlook(int? gap, int weeklyAverage, ChallengeResponse? challenge)
    {
        if (gap is null || challenge is null || challenge.HasEnded)
        {
            return null;
        }

        var days = challenge.DaysLeft;
        var atCurrentPace = (int)Math.Round(weeklyAverage / 7d * days);

        return new GapOutlookResponse(
            gap <= atCurrentPace,
            atCurrentPace <= 0 || gap > atCurrentPace * 3,
            atCurrentPace,
            days);
    }

    private async Task<RankChangeResponse?> RankChangeAsync(
        Guid id, List<Standing> board, int position, CancellationToken cancellationToken)
    {
        var cutoff = clock.GetUtcNow().UtcDateTime - RecentWindow;
        var lastWeek = await standings.AsOfAsync(cutoff, cancellationToken);

        var previous = lastWeek.FindIndex(standing => standing.UserId == id);

        if (position < 0 || previous < 0 || previous == position)
        {
            return null;
        }

        var earnedSince = await db.Activities
            .Where(a => a.UserId == id && a.OccurredAtUtc >= cutoff)
            .SumAsync(a => a.Points, cancellationToken);

        var swapped = board.ElementAtOrDefault(previous);

        var moverPoints = swapped is null
            ? (int?)null
            : await db.Activities
                .Where(a => a.UserId == swapped.UserId && a.OccurredAtUtc >= cutoff)
                .SumAsync(a => a.Points, cancellationToken);

        return new RankChangeResponse(
            previous + 1,
            position + 1,
            earnedSince,
            swapped is null ? null : $"{swapped.FirstName} {swapped.LastName}",
            moverPoints);
    }

    private async Task<int> ActiveDaysAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken) =>
        await db.Activities
            .Where(a => a.UserId == id && a.OccurredAtUtc >= nowUtc - RecentWindow)
            .Select(a => a.LocalDate)
            .Distinct()
            .CountAsync(cancellationToken);

    private async Task<DayTotalResponse?> BestDayAsync(Guid id, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var days = await db.Activities
            .Where(a => a.UserId == id && a.OccurredAtUtc >= nowUtc - HabitWindow)
            .GroupBy(a => a.LocalDate)
            .Select(g => new { Date = g.Key, Points = g.Sum(a => a.Points) })
            .ToListAsync(cancellationToken);

        return days
            .OrderByDescending(day => day.Points)
            .ThenBy(day => day.Date)
            .Select(day => new DayTotalResponse(day.Date, day.Points))
            .FirstOrDefault();
    }

    private async Task<string?> BestWeekdayAsync(Guid id, CancellationToken cancellationToken)
    {
        var days = await db.Activities
            .Where(a => a.UserId == id)
            .Select(a => new { a.LocalDate, a.Points })
            .ToListAsync(cancellationToken);

        return days.Count < ActivitiesBeforeAPattern
            ? null
            : days
                .GroupBy(day => day.LocalDate.DayOfWeek)
                .OrderByDescending(weekday => weekday.Sum(day => day.Points))

                .ThenBy(weekday => weekday.Key)
                .Select(weekday => weekday.Key.ToString())
                .First();
    }

    private static int WeeklyAverage(int points, DateTime? startedUtc, DateTime nowUtc)
    {
        if (startedUtc is null)
        {
            return 0;
        }

        var weeks = Math.Max(1d, (nowUtc - startedUtc.Value).TotalDays / 7d);

        return (int)Math.Round(points / weeks);
    }

    private static CoachEffortResponse Describe(EarningEffort effort) =>
        new(SportWire.ToWire(effort.Sport),
            SportWire.Label(effort.Sport),
            effort.Quantity,

            effort.Metric switch
            {
                MetricKind.Distance => "km",
                MetricKind.Duration => "min",
                _ => "steps",
            });
}
