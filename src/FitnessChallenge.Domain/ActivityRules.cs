namespace FitnessChallenge.Domain;

/// <summary>How one activity type is measured, and what one unit of it is worth.</summary>
public sealed record ActivityRule(Sport Sport, MetricKind Metric, decimal PointsPerUnit);

/// <summary>
/// The single source of truth for scoring. Point calculation, submission validation and the
/// sport list the API serves are all derived from this table, so adding an activity type is
/// one entry here and no change anywhere else.
/// </summary>
public static class ActivityRules
{
    private static readonly ActivityRule[] Table =
    [
        new(Sport.Running,    MetricKind.Distance, 100m),   // per kilometre
        new(Sport.Walking,    MetricKind.Distance, 50m),
        new(Sport.Cycling,    MetricKind.Distance, 25m),
        new(Sport.Swimming,   MetricKind.Duration, 15m),    // per whole minute
        new(Sport.Gym,        MetricKind.Duration, 5m),
        new(Sport.DailySteps, MetricKind.Count,    0.01m),  // 100 steps = 1 point
    ];

    public static IReadOnlyList<ActivityRule> All => Table;

    public static ActivityRule For(Sport sport) => Table.Single(rule => rule.Sport == sport);
}
