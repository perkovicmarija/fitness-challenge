namespace FitnessChallenge.Api.Data;

public class User
{
    public Guid Id { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    /// <summary>
    /// The name reduced to a comparable form by <see cref="PersonName.Normalize"/>. The original
    /// spelling is kept in <see cref="FirstName"/> and <see cref="LastName"/>; only the
    /// comparison is normalized.
    /// </summary>
    public required string NormalizedName { get; set; }
}
