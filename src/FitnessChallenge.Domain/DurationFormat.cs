using System.Globalization;

namespace FitnessChallenge.Domain;

/// <summary>
/// Parses the <c>mm:ss</c> duration format the API accepts.
/// </summary>
public static class DurationFormat
{
    /// <summary>
    /// Reads <paramref name="text"/> as <c>mm:ss</c> and yields the whole minutes it contains.
    /// </summary>
    /// <remarks>
    /// Seconds are deliberately discarded rather than rounded: the assignment awards points for
    /// completed minutes only, so "1:55" is one minute. Minutes may exceed 59 ("90:30" is an hour
    /// and a half); seconds may not. A duration of zero is rejected, but "0:59" is accepted and
    /// simply earns nothing.
    /// </remarks>
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

    // NumberStyles.None rejects signs, whitespace and thousands separators, so "-1:00" and
    // " 5:30" fail here rather than being quietly accepted.
    private static bool TryParseComponent(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
