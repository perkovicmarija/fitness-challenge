namespace FitnessChallenge.Api.DTO;

public sealed record SportResponse(string? Sport, string Label, string Metric, string Unit, decimal PointsPerUnit);
