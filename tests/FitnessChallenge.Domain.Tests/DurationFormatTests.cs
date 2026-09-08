namespace FitnessChallenge.Domain.Tests;

public class DurationFormatTests
{
    [Theory]
    [InlineData("1:55", 1)]     // the assignment's example: seconds never round up
    [InlineData("0:59", 0)]     // under a minute is nothing completed
    [InlineData("10:00", 10)]
    [InlineData("90:30", 90)]   // minutes may pass 59; this is an hour and a half
    public void ReadsCompletedMinutes(string text, int expected)
    {
        Assert.True(DurationFormat.TryParseWholeMinutes(text, out var minutes));
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData("5:75")]    // seconds must be under 60
    [InlineData("5")]       // both components are required
    [InlineData("1:2:3")]
    [InlineData("0:00")]    // a zero duration is not an activity
    [InlineData("-1:00")]
    [InlineData(" 5:30")]   // no surrounding whitespace
    [InlineData("five:00")]
    [InlineData("")]
    [InlineData(null)]
    public void RejectsMalformedDurations(string? text)
    {
        Assert.False(DurationFormat.TryParseWholeMinutes(text, out _));
    }
}
