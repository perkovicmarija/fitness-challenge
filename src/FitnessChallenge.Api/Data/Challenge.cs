namespace FitnessChallenge.Api.Data;

public class Challenge
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly StartsOn { get; set; }

    public DateOnly EndsOn { get; set; }
}
