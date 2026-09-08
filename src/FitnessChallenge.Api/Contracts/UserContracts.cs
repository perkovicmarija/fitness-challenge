using System.ComponentModel.DataAnnotations;

namespace FitnessChallenge.Api.Contracts;

public sealed record RegisterUserRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;
}

public sealed record RegisteredUserResponse(Guid Id);

public sealed record UserResponse(Guid Id, string FirstName, string LastName);
