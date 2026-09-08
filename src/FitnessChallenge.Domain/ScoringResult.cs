namespace FitnessChallenge.Domain;

/// <summary>
/// The outcome of scoring a submitted activity: either the points it earned, or the reason the
/// submission was rejected.
/// </summary>
public sealed record ScoringResult
{
    private ScoringResult(bool ok, int points, string? error)
    {
        Ok = ok;
        Points = points;
        Error = error;
    }

    public bool Ok { get; }

    public int Points { get; }

    /// <summary>Why the submission was rejected, or <c>null</c> when it was accepted.</summary>
    public string? Error { get; }

    public static ScoringResult Success(int points) => new(true, points, null);

    public static ScoringResult Failure(string error) => new(false, 0, error);
}
