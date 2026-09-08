using FitnessChallenge.Api.Contracts;
using FitnessChallenge.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessChallenge.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(FitnessDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<RegisteredUserResponse>> Register(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = PersonName.Normalize(request.FirstName, request.LastName);

        if (normalizedName.Length == 0)
        {
            ModelState.AddModelError(nameof(request.FirstName), "First and last name cannot be blank.");
            return ValidationProblem(ModelState);
        }

        // 409 rather than 400: the request is well formed, it just collides with someone who is
        // already registered.
        if (await db.Users.AnyAsync(u => u.NormalizedName == normalizedName, cancellationToken))
        {
            return Problem(
                detail: $"A user named {request.FirstName} {request.LastName} is already registered.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var user = new Data.User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            NormalizedName = normalizedName,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new RegisteredUserResponse(user.Id));
    }

    /// <summary>The dashboard is a view of one user, so the frontend needs someone to choose.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetAll(CancellationToken cancellationToken) =>
        await db.Users
            .OrderBy(u => u.NormalizedName)
            .Select(u => new UserResponse(u.Id, u.FirstName, u.LastName))
            .ToListAsync(cancellationToken);
}
