namespace FitnessChallenge.Domain;

public sealed record EarningEffort(Sport Sport, MetricKind Metric, decimal Quantity);

public static class PointsGap
{

    public static IReadOnlyList<EarningEffort> WaysToEarn(int points) =>
        points <= 0
            ? []
            : ActivityRules.All
                .Select(rule => new EarningEffort(rule.Sport, rule.Metric, QuantityFor(points, rule)))
                .ToList();

    private static decimal QuantityFor(int points, ActivityRule rule)
    {
        var exact = points / rule.PointsPerUnit;

        return rule.Metric == MetricKind.Distance
            ? decimal.Ceiling(exact * 100m) / 100m
            : decimal.Ceiling(exact);
    }
}
