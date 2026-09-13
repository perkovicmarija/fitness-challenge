namespace FitnessChallenge.Api.Coach;

public sealed class CoachOptions
{
    public const string SectionName = "Coach";

    public string? Endpoint { get; set; }

    public string? ApiKey { get; set; }

    public string? Deployment { get; set; }

    public string ApiVersion { get; set; } = "2024-10-21";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(Deployment)

        && Uri.TryCreate(Endpoint, UriKind.Absolute, out _);

    public Uri ResourceUri => new(new Uri(Endpoint!), "/");
}
