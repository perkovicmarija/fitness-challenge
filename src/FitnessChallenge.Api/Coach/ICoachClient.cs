namespace FitnessChallenge.Api.Coach;

public sealed record CoachMessage(string Role, string Text);

public interface ICoachClient
{
    Task<string> ReplyAsync(
        string briefing,
        IReadOnlyList<CoachMessage> conversation,
        CancellationToken cancellationToken);
}
