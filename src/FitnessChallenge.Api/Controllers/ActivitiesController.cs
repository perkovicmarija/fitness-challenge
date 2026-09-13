using FitnessChallenge.Api.DTO;
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
        if (!SportWire.TryParse(request.Sport, out var sport))
        {
            ModelState.AddModelError(nameof(request.Sport), $"'{request.Sport}' is not a sport this challenge scores.");
            return ValidationProblem(ModelState);
        }

        if (!Iso8601.TryParseWithOffset(request.Datetime, out var occurredAt))
        {
            ModelState.AddModelError(nameof(request.Datetime), "datetime must be ISO 8601 and carry an explicit offset, such as 'Z' or '+02:00'.");
            return ValidationProblem(ModelState);
        }

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

        var activity = Activity.Log(
            request.UserId, occurredAt, sport, request.Distance, request.Duration, request.Steps, scored.Points);

        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ActivityResponse(
            activity.Id,
            activity.UserId,
            occurredAt,
            activity.LocalDate,
            SportWire.ToWire(sport),
            activity.Distance,
            activity.Duration,
            activity.Steps,
            activity.Points));
    }
}
