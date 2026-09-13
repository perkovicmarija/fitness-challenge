using FitnessChallenge.Domain;

namespace FitnessChallenge.Api.Data;

public class Activity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public DateOnly LocalDate { get; set; }

    public int UtcOffsetMinutes { get; set; }

    public Sport Sport { get; set; }

    public decimal? Distance { get; set; }

    public string? Duration { get; set; }

    public int? Steps { get; set; }

    public int Points { get; set; }

    public static Activity Log(
        Guid userId,
        DateTimeOffset occurredAt,
        Sport sport,
        decimal? distance,
        string? duration,
        int? steps,
        int points) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OccurredAtUtc = occurredAt.UtcDateTime,
            LocalDate = DateOnly.FromDateTime(occurredAt.DateTime),
            UtcOffsetMinutes = (int)occurredAt.Offset.TotalMinutes,
            Sport = sport,
            Distance = distance,
            Duration = duration,
            Steps = steps,
            Points = points,
        };
}
