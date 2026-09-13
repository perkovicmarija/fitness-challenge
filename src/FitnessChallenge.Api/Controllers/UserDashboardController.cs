using FitnessChallenge.Api.DTO;
using FitnessChallenge.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessChallenge.Api.Controllers;

[ApiController]
[Route("api/users/{id:guid}")]
public class UserDashboardController(FitnessDbContext db, TimeProvider clock) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(
        Guid id, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id, cancellationToken))
        {
            return NotFound();
        }

        var selected = InRange(id, from, to);

        var perDay = await selected
            .GroupBy(a => a.LocalDate)
            .OrderBy(g => g.Key)
            .Select(g => new DayTotalResponse(g.Key, g.Sum(a => a.Points)))
            .ToListAsync(cancellationToken);

        var mine = await TotalsBySportAsync(selected, cancellationToken);
        var myTotal = mine.Sum(row => row.Points);

        var field = await TotalsBySportAsync(db.Activities, cancellationToken);
        var fieldTotal = field.Sum(row => row.Points);

        return new DashboardResponse(
            id,
            myTotal,
            await CurrentStreakAsync(id, cancellationToken),
            perDay,
            mine.Select(row => new SportTotalResponse(SportWire.ToWire(row.Sport), row.Points, Share(row.Points, myTotal)))
                .OrderByDescending(row => row.Points)
                .ToList(),
            field.Select(row => new SportShareResponse(SportWire.ToWire(row.Sport), Share(row.Points, fieldTotal)))
                .ToList());
    }

    [HttpGet("activities")]
    public async Task<ActionResult<IReadOnlyList<ActivityResponse>>> GetActivities(
        Guid id, DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id, cancellationToken))
        {
            return NotFound();
        }

        var activities = await InRange(id, from, to)
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        return activities
            .Select(a => new ActivityResponse(
                a.Id,
                a.UserId,

                new DateTimeOffset(a.OccurredAtUtc, TimeSpan.Zero).ToOffset(TimeSpan.FromMinutes(a.UtcOffsetMinutes)),
                a.LocalDate,
                SportWire.ToWire(a.Sport),
                a.Distance,
                a.Duration,
                a.Steps,
                a.Points))
            .ToList();
    }

    private IQueryable<Activity> InRange(Guid userId, DateOnly? from, DateOnly? to)
    {
        var activities = db.Activities.Where(a => a.UserId == userId);

        if (from is not null)
        {
            activities = activities.Where(a => a.LocalDate >= from);
        }

        return to is null ? activities : activities.Where(a => a.LocalDate <= to);
    }

    private static Task<List<SportTotal>> TotalsBySportAsync(IQueryable<Activity> activities, CancellationToken cancellationToken) =>
        activities
            .GroupBy(a => a.Sport)
            .Select(g => new SportTotal(g.Key, g.Sum(a => a.Points)))
            .ToListAsync(cancellationToken);

    private async Task<int> CurrentStreakAsync(Guid userId, CancellationToken cancellationToken)
    {
        var lastOffsetMinutes = await db.Activities
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAtUtc)
            .Select(a => (int?)a.UtcOffsetMinutes)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastOffsetMinutes is null)
        {
            return 0;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime.AddMinutes(lastOffsetMinutes.Value));

        var activeDays = (await db.Activities
                .Where(a => a.UserId == userId)
                .Select(a => a.LocalDate)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var day = activeDays.Contains(today) ? today : today.AddDays(-1);

        var streak = 0;

        while (activeDays.Contains(day))
        {
            streak++;
            day = day.AddDays(-1);
        }

        return streak;
    }

    private static double Share(int points, int total) => total == 0 ? 0 : (double)points / total;

    private sealed record SportTotal(Domain.Sport Sport, int Points);
}
