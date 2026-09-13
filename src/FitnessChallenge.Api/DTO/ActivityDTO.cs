using System.ComponentModel.DataAnnotations;

namespace FitnessChallenge.Api.DTO;

public sealed record LogActivityRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public string Datetime { get; init; } = string.Empty;

    public string? Sport { get; init; }

    public decimal? Distance { get; init; }

    public string? Duration { get; init; }

    public int? Steps { get; init; }
}

public sealed record ActivityResponse(
    Guid Id,
    Guid UserId,
    DateTimeOffset OccurredAt,
    DateOnly LocalDate,
    string? Sport,
    decimal? Distance,
    string? Duration,
    int? Steps,
    int Points);
