using FitnessChallenge.Api.DTO;
using Microsoft.EntityFrameworkCore;

namespace FitnessChallenge.Api.Data;

public sealed class ChallengeQuery(FitnessDbContext db, TimeProvider clock)
{

    public async Task<ChallengeResponse?> CurrentAsync(CancellationToken cancellationToken)
    {
        var challenge = await db.Challenges.FirstOrDefaultAsync(cancellationToken);

        if (challenge is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var totalDays = challenge.EndsOn.DayNumber - challenge.StartsOn.DayNumber + 1;

        return new ChallengeResponse(
            challenge.Name,
            challenge.StartsOn,
            challenge.EndsOn,
            Math.Clamp(today.DayNumber - challenge.StartsOn.DayNumber + 1, 0, totalDays),
            totalDays,
            Math.Max(0, challenge.EndsOn.DayNumber - today.DayNumber),
            today > challenge.EndsOn);
    }
}
