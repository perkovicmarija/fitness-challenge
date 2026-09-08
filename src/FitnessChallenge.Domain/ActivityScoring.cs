namespace FitnessChallenge.Domain;

/// <summary>
/// Validates a submitted activity against <see cref="ActivityRules"/> and converts it to points.
/// </summary>
/// <remarks>
/// Validation and scoring are one step on purpose: both answer the same question — does this
/// submission carry the one measurement its activity type is scored on? Splitting them would
/// allow scoring to be called on a submission that was never checked.
/// </remarks>
public static class ActivityScoring
{
    public static ScoringResult Score(Sport sport, decimal? distance, string? duration, int? steps)
    {
        var rule = ActivityRules.For(sport);

        if (!CarriesOnlyItsOwnMeasurement(rule.Metric, distance, duration, steps))
        {
            return ScoringResult.Failure(
                $"A {sport} activity is measured by {Measurement(rule.Metric)}, which must be the only measurement supplied.");
        }

        switch (rule.Metric)
        {
            case MetricKind.Distance:
                return distance!.Value > 0
                    ? Award(distance.Value, rule)
                    : ScoringResult.Failure("distance must be greater than zero.");

            case MetricKind.Duration:
                return DurationFormat.TryParseWholeMinutes(duration, out var minutes)
                    ? Award(minutes, rule)
                    : ScoringResult.Failure("duration must be mm:ss, with seconds between 0 and 59.");

            default:
                return steps!.Value > 0
                    ? Award(steps.Value, rule)
                    : ScoringResult.Failure("steps must be greater than zero.");
        }
    }

    /// <remarks>
    /// Every metric awards <c>floor(quantity × rate)</c>. The rounding the assignment describes
    /// for duration and steps has already happened by this point: "mm:ss" parses to whole
    /// minutes, and a step is worth 0.01 points so incomplete hundreds fall away in the floor.
    /// Distance is the only metric whose fraction survives this far, which is exactly what the
    /// assignment asks for.
    /// </remarks>
    private static ScoringResult Award(decimal quantity, ActivityRule rule)
    {
        var points = decimal.Floor(quantity * rule.PointsPerUnit);

        return points > int.MaxValue
            ? ScoringResult.Failure("The measurement is too large to be a real activity.")
            : ScoringResult.Success((int)points);
    }

    // Rejects the assignment's own invalid example: swimming submitted with a distance.
    private static bool CarriesOnlyItsOwnMeasurement(MetricKind metric, decimal? distance, string? duration, int? steps) =>
        metric switch
        {
            MetricKind.Distance => distance is not null && duration is null && steps is null,
            MetricKind.Duration => duration is not null && distance is null && steps is null,
            _ => steps is not null && distance is null && duration is null,
        };

    private static string Measurement(MetricKind metric) =>
        metric switch
        {
            MetricKind.Distance => "distance",
            MetricKind.Duration => "duration",
            _ => "steps",
        };
}
