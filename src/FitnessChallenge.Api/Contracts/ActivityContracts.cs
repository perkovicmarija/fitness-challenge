using System.ComponentModel.DataAnnotations;

namespace FitnessChallenge.Api.Contracts;

public sealed record LogActivityRequest
{
    [Required]
    public Guid UserId { get; init; }

    /// <summary>ISO 8601, and it must carry an explicit offset. See <see cref="Iso8601"/>.</summary>
    [Required]
    public string Datetime { get; init; } = string.Empty;

    /// <summary>Absent means daily steps, as the assignment specifies.</summary>
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
