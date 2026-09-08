namespace FitnessChallenge.Domain;

/// <summary>
/// How an activity is measured. Determines the one measurement a submission must carry.
/// </summary>
public enum MetricKind
{
    /// <summary>Kilometres, decimal.</summary>
    Distance,

    /// <summary>Whole minutes, parsed from a <c>mm:ss</c> string.</summary>
    Duration,

    /// <summary>A number of steps.</summary>
    Count,
}
