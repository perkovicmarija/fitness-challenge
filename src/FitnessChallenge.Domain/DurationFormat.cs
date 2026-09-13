using System.Globalization;

namespace FitnessChallenge.Domain;

public static class DurationFormat
{

    public static bool TryParseWholeMinutes(string? text, out int wholeMinutes)
    {
        wholeMinutes = 0;

        if (text is null)
        {
            return false;
        }

        var parts = text.Split(':');

        if (parts.Length != 2
            || !TryParseComponent(parts[0], out var minutes)
            || !TryParseComponent(parts[1], out var seconds)
            || seconds > 59
            || (minutes == 0 && seconds == 0))
        {
            return false;
        }

        wholeMinutes = minutes;
        return true;
    }

    private static bool TryParseComponent(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
