using FitnessChallenge.Api.Contracts;
using FitnessChallenge.Api.Data;
using FitnessChallenge.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessChallenge.Api.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivitiesController(FitnessDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ActivityResponse>> Log(LogActivityRequest request, CancellationToken cancellationToken)
    {
        if (!TryResolveSport(request.Sport, out var sport))
        {
            ModelState.AddModelError(nameof(request.Sport), $"'{request.Sport}' is not a sport this challenge scores.");
            return ValidationProblem(ModelState);
        }

        if (!Iso8601.TryParseWithOffset(request.Datetime, out var occurredAt))
        {
            ModelState.AddModelError(nameof(request.Datetime), "datetime must be ISO 8601 and carry an explicit offset, such as 'Z' or '+02:00'.");
            return ValidationProblem(ModelState);
        }

        // An unresolvable user makes the body invalid, so this is 400 and not 404 — the endpoint
        // itself exists. A missing user named in the path is a 404; see docs/contract.md.
        if (!await db.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.UserId), "No user is registered with that id.");
            return ValidationProblem(ModelState);
        }

        var scored = ActivityScoring.Score(sport, request.Distance, request.Duration, request.Steps);

        if (!scored.Ok)
        {
            ModelState.AddModelError("activity", scored.Error!);
            return ValidationProblem(ModelState);
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            OccurredAtUtc = occurredAt.UtcDateTime,
            LocalDate = DateOnly.FromDateTime(occurredAt.DateTime),
            Sport = sport,
            Distance = request.Distance,
            Duration = request.Duration,
            Steps = request.Steps,
            Points = scored.Points,
        };

        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ActivityResponse(
            activity.Id,
            activity.UserId,
            occurredAt,
            activity.LocalDate,
            request.Sport,
            activity.Distance,
            activity.Duration,
            activity.Steps,
            activity.Points));
    }

    /// <summary>An absent sport means daily steps; "dailySteps" itself is not a wire value.</summary>
    private static bool TryResolveSport(string? value, out Sport sport)
    {
        if (value is null)
        {
            sport = Sport.DailySteps;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out sport) && sport != Sport.DailySteps;
    }
}
