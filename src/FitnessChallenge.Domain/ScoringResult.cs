namespace FitnessChallenge.Domain;

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

    public string? Error { get; }

    public static ScoringResult Success(int points) => new(true, points, null);

    public static ScoringResult Failure(string error) => new(false, 0, error);
}
