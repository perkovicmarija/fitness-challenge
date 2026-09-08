using System.Globalization;

namespace FitnessChallenge.Api.Contracts;

public static class Iso8601
{
    /// <summary>
    /// Parses an ISO 8601 timestamp and requires it to carry an explicit offset — "Z" or
    /// "+02:00". A naive timestamp such as "2026-06-30T10:30:00" is rejected: without an offset
    /// the calendar day on the user's own clock is unknowable, and every per-day figure in the
    /// dashboard depends on it.
    /// </summary>
    public static bool TryParseWithOffset(string? text, out DateTimeOffset value)
    {
        value = default;

        // Round-tripping as DateTime first is what reveals a missing offset: only then is Kind
        // left Unspecified. DateTimeOffset.TryParse would silently assume the server's zone.
        if (text is null
            || !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var probe)
            || probe.Kind == DateTimeKind.Unspecified)
        {
            return false;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
