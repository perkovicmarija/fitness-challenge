using FitnessChallenge.Domain;

namespace FitnessChallenge.Api.Data;

public class Activity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>The instant it happened. Everything that compares users uses this.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// The date on the user's own clock, taken from the offset they submitted. Everything that
    /// groups by day — the volume chart, the heatmap, the streak — uses this instead of the UTC
    /// date, so an activity at 01:30 lands on the day the user actually experienced.
    /// </summary>
    public DateOnly LocalDate { get; set; }

    public Sport Sport { get; set; }

    /// <summary>The measurement exactly as submitted, kept for display. Only one is ever set.</summary>
    public decimal? Distance { get; set; }

    public string? Duration { get; set; }

    public int? Steps { get; set; }

    /// <summary>Calculated once, here, at ingest. The read side never recomputes it.</summary>
    public int Points { get; set; }
}
