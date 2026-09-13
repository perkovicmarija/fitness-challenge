using FitnessChallenge.Domain;

namespace FitnessChallenge.Api.Data;

public static class DemoData
{
    private const int TrendWindowDays = 7;

    private const int RandomSeed = 20260915;

    private const int ChallengeDays = 30;

    private const int ChallengeDaysLeft = 6;

    private static readonly Competitor[] Competitors =
    [

        new("Anna", "Wilson", TimeSpan.FromHours(2), 0.80, 1.0, 140,
            [(Sport.Running, 0.45), (Sport.Walking, 0.3), (Sport.DailySteps, 0.25)]),

        new("Daniel", "Carter", TimeSpan.FromHours(2), 0.54, 0.0, 140,
            [(Sport.Cycling, 0.5), (Sport.Running, 0.3), (Sport.Gym, 0.2)]),

        new("Alex", "Martin", TimeSpan.FromHours(-5), 0.62, 4.0, 140,
            [(Sport.Swimming, 0.4), (Sport.Cycling, 0.35), (Sport.Gym, 0.25)]),

        new("Sarah", "Miller", TimeSpan.FromHours(1), 0.79, 1.0, 140,
            [(Sport.Walking, 0.4), (Sport.Running, 0.35), (Sport.DailySteps, 0.25)]),

        new("Marco", "Rossi", TimeSpan.FromHours(1), 0.585, 1.0, 140,
            [(Sport.Cycling, 0.4), (Sport.Walking, 0.35), (Sport.Swimming, 0.25)]),

        new("Emma", "Davis", TimeSpan.FromHours(2), 1.4, 1.0, 5,
            [(Sport.Running, 0.5), (Sport.Gym, 0.3), (Sport.Cycling, 0.2)]),
    ];

    private static readonly Dictionary<Sport, (double Min, double Max)> SessionSize = new()
    {
        [Sport.Running] = (3, 10),
        [Sport.Walking] = (4, 12),
        [Sport.Cycling] = (15, 45),
        [Sport.Swimming] = (25, 50),
        [Sport.Gym] = (45, 90),
        [Sport.DailySteps] = (6_000, 18_000),
    };

    public static void Populate(FitnessDbContext db, TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        if (!db.Challenges.Any())
        {
            db.Challenges.Add(new Challenge
            {
                Id = Guid.NewGuid(),
                Name = "Move More",
                StartsOn = today.AddDays(ChallengeDaysLeft - ChallengeDays + 1),
                EndsOn = today.AddDays(ChallengeDaysLeft),
            });

            db.SaveChanges();
        }

        if (db.Users.Any())
        {
            return;
        }

        foreach (var (competitor, index) in Competitors.Select((competitor, index) => (competitor, index)))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = competitor.FirstName,
                LastName = competitor.LastName,
                NormalizedName = PersonName.Normalize(competitor.FirstName, competitor.LastName),
            };

            db.Users.Add(user);

            db.Activities.AddRange(HistoryFor(user.Id, competitor, now, new Random(RandomSeed + index)));
        }

        db.SaveChanges();
    }

    private static IEnumerable<Activity> HistoryFor(
        Guid userId, Competitor competitor, DateTimeOffset now, Random random)
    {
        var localNow = now.ToOffset(competitor.Offset);
        var today = DateOnly.FromDateTime(localNow.DateTime);

        for (var daysAgo = competitor.HistoryDays - 1; daysAgo >= 0; daysAgo--)
        {

            var latestHour = daysAgo == 0 ? localNow.Hour : 21;

            if (latestHour < 7)
            {
                continue;
            }

            var rate = daysAgo < TrendWindowDays
                ? competitor.ActivitiesPerDay * competitor.FinalWeekFactor
                : competitor.ActivitiesPerDay;

            var count = (int)rate + (random.NextDouble() < rate - Math.Truncate(rate) ? 1 : 0);

            for (var i = 0; i < count; i++)
            {
                var when = new DateTimeOffset(
                    today.AddDays(-daysAgo).ToDateTime(new TimeOnly(random.Next(7, latestHour + 1), random.Next(0, 60))),
                    competitor.Offset);

                yield return Session(userId, when, Pick(random, competitor.Profile), random);
            }
        }
    }

    private static Activity Session(Guid userId, DateTimeOffset when, Sport sport, Random random)
    {
        var (min, max) = SessionSize[sport];
        var size = min + (random.NextDouble() * (max - min));

        decimal? distance = null;
        string? duration = null;
        int? steps = null;

        switch (ActivityRules.For(sport).Metric)
        {
            case MetricKind.Distance:
                distance = Math.Round((decimal)size, 2);
                break;

            case MetricKind.Duration:

                duration = $"{(int)size}:{random.Next(0, 60):00}";
                break;

            default:
                steps = (int)size;
                break;
        }

        var scored = ActivityScoring.Score(sport, distance, duration, steps);

        if (!scored.Ok)
        {
            throw new InvalidOperationException($"Demo data is not a valid activity: {scored.Error}");
        }

        return Activity.Log(userId, when, sport, distance, duration, steps, scored.Points);
    }

    private static Sport Pick(Random random, (Sport Sport, double Weight)[] profile)
    {
        var roll = random.NextDouble();
        var cumulative = 0.0;

        foreach (var (sport, weight) in profile)
        {
            cumulative += weight;

            if (roll <= cumulative)
            {
                return sport;
            }
        }

        return profile[^1].Sport;
    }

    private sealed record Competitor(
        string FirstName,
        string LastName,
        TimeSpan Offset,
        double ActivitiesPerDay,

        double FinalWeekFactor,
        int HistoryDays,
        (Sport Sport, double Weight)[] Profile);
}
