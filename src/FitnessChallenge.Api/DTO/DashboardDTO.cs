namespace FitnessChallenge.Api.DTO;

public sealed record DayTotalResponse(DateOnly Date, int Points);

public sealed record SportTotalResponse(string? Sport, int Points, double Share);

public sealed record SportShareResponse(string? Sport, double Share);

public sealed record DashboardResponse(
    Guid UserId,
    int TotalPoints,
    int CurrentStreakDays,
    IReadOnlyList<DayTotalResponse> PerDay,
    IReadOnlyList<SportTotalResponse> PerSport,
    IReadOnlyList<SportShareResponse> FieldAverage);
