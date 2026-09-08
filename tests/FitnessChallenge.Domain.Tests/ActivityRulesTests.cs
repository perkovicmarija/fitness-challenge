namespace FitnessChallenge.Domain.Tests;

public class ActivityRulesTests
{
    /// <summary>
    /// The rule table promises that adding an activity type is a single entry. This fails the
    /// build the moment a <see cref="Sport"/> is added without one, instead of throwing at
    /// runtime the first time somebody submits that activity.
    /// </summary>
    [Fact]
    public void EverySportHasExactlyOneRule()
    {
        foreach (var sport in Enum.GetValues<Sport>())
        {
            Assert.Single(ActivityRules.All, rule => rule.Sport == sport);
        }
    }
}
