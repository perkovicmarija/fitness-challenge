namespace FitnessChallenge.Domain.Tests;

public class PointsGapTests
{

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(340)]
    [InlineData(341)]
    [InlineData(2_140)]
    public void EveryQuantityEarnsAtLeastTheGap(int gap)
    {
        foreach (var effort in PointsGap.WaysToEarn(gap))
        {
            var scored = ActivityScoring.Score(
                effort.Sport,
                effort.Metric == MetricKind.Distance ? effort.Quantity : null,
                effort.Metric == MetricKind.Duration ? $"{(int)effort.Quantity}:00" : null,
                effort.Metric == MetricKind.Count ? (int)effort.Quantity : null);

            Assert.True(scored.Ok, scored.Error);
            Assert.True(
                scored.Points >= gap,
                $"{effort.Sport} {effort.Quantity} earns {scored.Points}, short of {gap}.");
        }
    }

    [Fact]
    public void ReadsTheRuleTableBackwards()
    {
        var ways = PointsGap.WaysToEarn(340).ToDictionary(effort => effort.Sport, effort => effort.Quantity);

        Assert.Equal(3.4m, ways[Sport.Running]);
        Assert.Equal(13.6m, ways[Sport.Cycling]);

        Assert.Equal(23m, ways[Sport.Swimming]);
        Assert.Equal(34_000m, ways[Sport.DailySteps]);
    }

    [Fact]
    public void LeadingTheFieldNeedsNoEffort() => Assert.Empty(PointsGap.WaysToEarn(0));
}
