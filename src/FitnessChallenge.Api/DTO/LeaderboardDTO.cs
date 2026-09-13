namespace FitnessChallenge.Api.DTO;

public sealed record LeaderboardEntryResponse(
    int Rank,
    Guid UserId,
    string FirstName,
    string LastName,
    int TotalPoints,
    int? PreviousRank,
    int? RankDelta,
    int? PointsToOvertake);

public sealed record ChallengeResponse(
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int Day,
    int TotalDays,
    int DaysLeft,
    bool HasEnded);

public sealed record LeaderboardResponse(
    ChallengeResponse? Challenge,
    int ActiveToday,
    IReadOnlyList<LeaderboardEntryResponse> Entries);
