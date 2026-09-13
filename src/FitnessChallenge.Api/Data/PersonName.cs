namespace FitnessChallenge.Api.Data;

public static class PersonName
{

    public static string Normalize(string firstName, string lastName) =>
        string.Join(
            ' ',
            $"{firstName} {lastName}".Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToUpperInvariant();
}
