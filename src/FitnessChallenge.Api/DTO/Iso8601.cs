using System.Globalization;

namespace FitnessChallenge.Api.DTO;

public static class Iso8601
{

    public static bool TryParseWithOffset(string? text, out DateTimeOffset value)
    {
        value = default;

        if (text is null
            || !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var probe)
            || probe.Kind == DateTimeKind.Unspecified)
        {
            return false;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
