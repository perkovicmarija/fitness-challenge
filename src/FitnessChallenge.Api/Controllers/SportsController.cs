using FitnessChallenge.Api.DTO;
using FitnessChallenge.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FitnessChallenge.Api.Controllers;

[ApiController]
[Route("api/sports")]
public class SportsController : ControllerBase
{

    [HttpGet]
    public IReadOnlyList<SportResponse> Get() =>
        ActivityRules.All
            .Select(rule => new SportResponse(
                SportWire.ToWire(rule.Sport),
                SportWire.Label(rule.Sport),
                rule.Metric.ToString().ToLowerInvariant(),
                SportWire.Unit(rule.Metric),
                rule.PointsPerUnit))
            .ToList();
}
