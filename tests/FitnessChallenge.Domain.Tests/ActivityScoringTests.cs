using System.Globalization;

namespace FitnessChallenge.Domain.Tests;

public class ActivityScoringTests
{
    [Theory]
    [InlineData(Sport.Walking, "1.55", 77)]
    [InlineData(Sport.Running, "42.195", 4219)]
    [InlineData(Sport.Cycling, "10", 250)]
    public void ScoresDistanceByFlooringTheAwardedPoints(Sport sport, string kilometres, int expected)
    {
        var result = ActivityScoring.Score(sport, Kilometres(kilometres), null, null);

        Assert.True(result.Ok);
        Assert.Equal(expected, result.Points);
    }

    [Fact]
    public void ScoresDistanceInDecimalBecauseFloatingPointLosesPoints()
    {
        Assert.Equal(28, (int)Math.Floor(0.29d * 100));

        var result = ActivityScoring.Score(Sport.Running, 0.29m, null, null);

        Assert.True(result.Ok);
        Assert.Equal(29, result.Points);
    }

    [Theory]
    [InlineData(Sport.Swimming, "1:55", 15)]
    [InlineData(Sport.Swimming, "90:30", 1350)]
    [InlineData(Sport.Gym, "10:00", 50)]
    [InlineData(Sport.Gym, "0:59", 0)]
    public void ScoresDurationByCompletedMinutes(Sport sport, string duration, int expected)
    {
        var result = ActivityScoring.Score(sport, null, duration, null);

        Assert.True(result.Ok);
        Assert.Equal(expected, result.Points);
    }

    [Theory]
    [InlineData(399, 3)]
    [InlineData(100, 1)]
    [InlineData(99, 0)]
    [InlineData(10_000, 100)]
    public void ScoresStepsByCompletedHundreds(int steps, int expected)
    {
        var result = ActivityScoring.Score(Sport.DailySteps, null, null, steps);

        Assert.True(result.Ok);
        Assert.Equal(expected, result.Points);
    }

    [Fact]
    public void RejectsTheAssignmentsOwnInvalidExample()
    {
        var result = ActivityScoring.Score(Sport.Swimming, 42.195m, null, null);

        Assert.False(result.Ok);
        Assert.Contains("duration", result.Error);
    }

    [Theory]
    [InlineData(Sport.Running, "1.5", null, null)]
    [InlineData(Sport.Walking, "1.5", null, null)]
    [InlineData(Sport.Cycling, "1.5", null, null)]
    [InlineData(Sport.Swimming, null, "5:00", null)]
    [InlineData(Sport.Gym, null, "5:00", null)]
    [InlineData(Sport.DailySteps, null, null, 500)]
    public void AcceptsTheOneMeasurementItIsScoredOn(Sport sport, string? distance, string? duration, int? steps)
    {
        Assert.True(ActivityScoring.Score(sport, Kilometres(distance), duration, steps).Ok);
    }

    [Theory]

    [InlineData(Sport.Running, null, "5:00", null)]
    [InlineData(Sport.Running, null, null, 500)]
    [InlineData(Sport.Walking, null, "5:00", null)]
    [InlineData(Sport.Walking, null, null, 500)]
    [InlineData(Sport.Cycling, null, "5:00", null)]
    [InlineData(Sport.Cycling, null, null, 500)]
    [InlineData(Sport.Swimming, "1.5", null, null)]
    [InlineData(Sport.Swimming, null, null, 500)]
    [InlineData(Sport.Gym, "1.5", null, null)]
    [InlineData(Sport.Gym, null, null, 500)]
    [InlineData(Sport.DailySteps, "1.5", null, null)]
    [InlineData(Sport.DailySteps, null, "5:00", null)]

    [InlineData(Sport.Running, "1.5", "5:00", null)]
    [InlineData(Sport.Walking, "1.5", null, 500)]
    [InlineData(Sport.Cycling, "1.5", "5:00", 500)]
    [InlineData(Sport.Swimming, "1.5", "5:00", null)]
    [InlineData(Sport.Gym, null, "5:00", 500)]
    [InlineData(Sport.DailySteps, "1.5", null, 500)]

    [InlineData(Sport.Running, null, null, null)]
    [InlineData(Sport.Walking, null, null, null)]
    [InlineData(Sport.Cycling, null, null, null)]
    [InlineData(Sport.Swimming, null, null, null)]
    [InlineData(Sport.Gym, null, null, null)]
    [InlineData(Sport.DailySteps, null, null, null)]
    public void RejectsAnythingOtherThanExactlyThatMeasurement(Sport sport, string? distance, string? duration, int? steps)
    {
        var result = ActivityScoring.Score(sport, Kilometres(distance), duration, steps);

        Assert.False(result.Ok);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData(Sport.Running, "0", null, null)]
    [InlineData(Sport.Walking, "-1.5", null, null)]
    [InlineData(Sport.DailySteps, null, null, 0)]
    [InlineData(Sport.DailySteps, null, null, -100)]
    public void RejectsMeasurementsThatAreNotPositive(Sport sport, string? distance, string? duration, int? steps)
    {
        Assert.False(ActivityScoring.Score(sport, Kilometres(distance), duration, steps).Ok);
    }

    private static decimal? Kilometres(string? value) =>
        value is null ? null : decimal.Parse(value, CultureInfo.InvariantCulture);
}
