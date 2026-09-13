using FitnessChallenge.Api.Coach;

namespace FitnessChallenge.Api.Tests;

public class CoachOptionsTests
{
    private static CoachOptions With(string? endpoint) =>
        new() { Endpoint = endpoint, ApiKey = "key", Deployment = "gpt-4o" };

    [Theory]
    [InlineData("https://example.openai.azure.com/")]
    [InlineData("https://example.openai.azure.com")]
    [InlineData("https://example.openai.azure.com/openai/deployments/gpt-4o/chat/completions?api-version=2025-01-01-preview")]
    public void AcceptsTheEndpointInEitherFormAzureOffersIt(string endpoint)
    {
        Assert.Equal("https://example.openai.azure.com/", With(endpoint).ResourceUri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-url")]
    public void AnEndpointThatIsNotAUrlLeavesTheCoachSwitchedOff(string? endpoint)
    {

        Assert.False(With(endpoint).IsConfigured);
    }

    [Fact]
    public void MissingCredentialsLeaveTheCoachSwitchedOff()
    {
        var options = new CoachOptions { Endpoint = "https://example.openai.azure.com/" };

        Assert.False(options.IsConfigured);
    }
}
