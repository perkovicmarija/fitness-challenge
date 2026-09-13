namespace FitnessChallenge.Domain.Tests;

public class DurationFormatTests
{
    [Theory]
    [InlineData("1:55", 1)]
    [InlineData("0:59", 0)]
    [InlineData("10:00", 10)]
    [InlineData("90:30", 90)]
    public void ReadsCompletedMinutes(string text, int expected)
    {
        Assert.True(DurationFormat.TryParseWholeMinutes(text, out var minutes));
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData("5:75")]
    [InlineData("5")]
    [InlineData("1:2:3")]
    [InlineData("0:00")]
    [InlineData("-1:00")]
    [InlineData(" 5:30")]
    [InlineData("five:00")]
    [InlineData("")]
    [InlineData(null)]
    public void RejectsMalformedDurations(string? text)
    {
        Assert.False(DurationFormat.TryParseWholeMinutes(text, out _));
    }
}
