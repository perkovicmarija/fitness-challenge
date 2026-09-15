using System.ComponentModel.DataAnnotations;

namespace FitnessChallenge.Api.DTO;

public static class CoachLimits
{
    public const int MaxMessages = 12;

    public const int MaxMessageLength = 500;
}

public sealed record CoachFactsResponse(
    Guid UserId,

    ChallengeResponse? Challenge,

    int? Rank,
    int Competitors,
    int TotalPoints,

    int? PointsToPassNextRank,
    int? PointsToTakeTheLead,

    RankChangeResponse? RankChange,
    GapOutlookResponse? Outlook,

    int ActiveDaysLastSevenDays,
    DayTotalResponse? BestDay,

    int? LeadOverNextRank,

    string? NextRankName,
    int ActivitiesLastSevenDays,
    int PointsLastSevenDays,

    int AverageWeeklyPoints,
    int DaysTrainedLastFourWeeks,

    string? BestWeekday,
    IReadOnlyList<CoachEffortResponse> WaysToPassNextRank,

    CoachEffortResponse? Recommended,
    IReadOnlyList<CoachSportResponse> Sports);

public sealed record GapOutlookResponse(bool WithinReach, bool Unlikely, int PointsAtCurrentPace, int DaysLeft);

public sealed record RankChangeResponse(
    int PreviousRank,
    int CurrentRank,
    int MyPointsThisWeek,
    string? Mover,
    int? MoverPointsThisWeek);

public sealed record CoachEffortResponse(string? Sport, string Label, decimal Quantity, string Unit);

public sealed record CoachSportResponse(string Label, int Points, int Sessions, int AveragePoints);

public sealed record CoachChatRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Send at least one message.")]
    [MaxLength(CoachLimits.MaxMessages, ErrorMessage = "The conversation is too long. Start a new one.")]
    public IReadOnlyList<CoachMessageRequest> Messages { get; init; } = [];
}

public sealed record CoachMessageRequest
{
    [Required]
    [RegularExpression("^(user|assistant)$", ErrorMessage = "role must be 'user' or 'assistant'.")]
    public string Role { get; init; } = string.Empty;

    [Required]
    [StringLength(CoachLimits.MaxMessageLength, MinimumLength = 1)]
    public string Text { get; init; } = string.Empty;
}

public sealed record CoachChatResponse(string Reply);
