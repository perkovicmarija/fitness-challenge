namespace FitnessChallenge.Domain;

public sealed record ActivityRule(Sport Sport, MetricKind Metric, decimal PointsPerUnit);

public static class ActivityRules
{
    private static readonly ActivityRule[] Table =
    [
        new(Sport.Running,    MetricKind.Distance, 100m),
        new(Sport.Walking,    MetricKind.Distance, 50m),
        new(Sport.Cycling,    MetricKind.Distance, 25m),
        new(Sport.Swimming,   MetricKind.Duration, 15m),
        new(Sport.Gym,        MetricKind.Duration, 5m),
        new(Sport.DailySteps, MetricKind.Count,    0.01m),
    ];

    public static IReadOnlyList<ActivityRule> All => Table;

    public static ActivityRule For(Sport sport) => Table.Single(rule => rule.Sport == sport);
}
