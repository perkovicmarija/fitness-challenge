namespace FitnessChallenge.Api.Data;

public class User
{
    public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string NormalizedName { get; set; }
}
