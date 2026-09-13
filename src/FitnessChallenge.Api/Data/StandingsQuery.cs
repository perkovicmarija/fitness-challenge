using Microsoft.EntityFrameworkCore;

namespace FitnessChallenge.Api.Data;

public sealed record Standing(Guid UserId, string FirstName, string LastName, int TotalPoints, DateTime ReachedAtUtc);

public sealed class StandingsQuery(FitnessDbContext db)
{

    public async Task<List<Standing>> AsOfAsync(DateTime? asOfUtc, CancellationToken cancellationToken)
    {
        var activities = db.Activities.AsQueryable();

        if (asOfUtc is not null)
        {
            activities = activities.Where(a => a.OccurredAtUtc <= asOfUtc);
        }

        var totals = await activities
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalPoints = g.Sum(a => a.Points),
                ReachedAtUtc = g.Max(a => a.OccurredAtUtc),
            })
            .ToListAsync(cancellationToken);

        var names = await db.Users
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return totals
            .Select(total => new Standing(
                total.UserId,
                names[total.UserId].FirstName,
                names[total.UserId].LastName,
                total.TotalPoints,
                total.ReachedAtUtc))
            .OrderByDescending(s => s.TotalPoints)

            .ThenBy(s => s.ReachedAtUtc)
            .ThenBy(s => s.UserId)
            .ToList();
    }
}
