using System.Net;
using System.Net.Http.Json;
using FitnessChallenge.Api.Contracts;

namespace FitnessChallenge.Api.Tests;

public class UsersEndpointTests
{
    [Fact]
    public async Task RegisteringAUserReturnsTheirId()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { firstName = "Ana", lastName = "Horvat" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<RegisteredUserResponse>();
        Assert.NotEqual(Guid.Empty, created!.Id);
    }

    [Fact]
    public async Task RegisteringTheSameNameTwiceConflicts()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/users", new { firstName = "Ana", lastName = "Horvat" });
        var second = await client.PostAsJsonAsync("/api/users", new { firstName = "Ana", lastName = "Horvat" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    /// <summary>
    /// Casing and stray whitespace must not be enough to register the same person twice, or the
    /// leaderboard shows them as two competitors.
    /// </summary>
    [Theory]
    [InlineData("ana", "horvat")]
    [InlineData("  Ana  ", " Horvat ")]
    [InlineData("ANA", "HoRvAt")]
    public async Task RegisteringAVariantOfAnExistingNameConflicts(string firstName, string lastName)
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/users", new { firstName = "Ana", lastName = "Horvat" });
        var second = await client.PostAsJsonAsync("/api/users", new { firstName, lastName });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("", "Horvat")]
    [InlineData("   ", "Horvat")]
    [InlineData("Ana", "")]
    public async Task RegisteringABlankNameIsRejected(string firstName, string lastName)
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { firstName, lastName });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RegisteredUsersAreListedWithTheSpellingTheyChose()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/users", new { firstName = "  Ana ", lastName = "de Vries" });

        var users = await client.GetFromJsonAsync<List<UserResponse>>("/api/users");

        var user = Assert.Single(users!);
        Assert.Equal("Ana", user.FirstName);
        Assert.Equal("de Vries", user.LastName);
    }
}
