namespace FitnessChallenge.Api.Data;

public static class PersonName
{
    /// <summary>
    /// Reduces a name to a comparable form: surrounding and repeated whitespace collapse to
    /// single spaces, and case is ignored. Without this, "Ana Horvat" and "ana  horvat" both
    /// register and the same person appears twice on the leaderboard.
    /// </summary>
    public static string Normalize(string firstName, string lastName) =>
        string.Join(
            ' ',
            $"{firstName} {lastName}".Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();
}
