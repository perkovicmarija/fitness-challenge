namespace FitnessChallenge.Domain.Tests;

public class ActivityRulesTests
{

    [Fact]
    public void EverySportHasExactlyOneRule()
    {
        foreach (var sport in Enum.GetValues<Sport>())
        {
            Assert.Single(ActivityRules.All, rule => rule.Sport == sport);
        }
    }
}
