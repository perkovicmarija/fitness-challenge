using FitnessChallenge.Domain;

namespace FitnessChallenge.Api.DTO;

public static class SportWire
{
    public static string? ToWire(Sport sport) =>
        sport == Sport.DailySteps
            ? null
            : char.ToLowerInvariant(sport.ToString()[0]) + sport.ToString()[1..];

    public static bool TryParse(string? value, out Sport sport)
    {
        if (value is null)
        {
            sport = Sport.DailySteps;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out sport) && sport != Sport.DailySteps;
    }

    public static string Label(Sport sport) => sport == Sport.DailySteps ? "Daily steps" : sport.ToString();

    public static string Unit(MetricKind metric) =>
        metric switch
        {
            MetricKind.Distance => "km",
            MetricKind.Duration => "mm:ss",
            _ => "steps",
        };
}
