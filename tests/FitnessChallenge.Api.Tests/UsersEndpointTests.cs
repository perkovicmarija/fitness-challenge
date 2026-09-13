using System.Net;
using System.Net.Http.Json;
using FitnessChallenge.Api.DTO;

namespace FitnessChallenge.Api.Tests;

public class UsersEndpointTests
{
    [Fact]
    public async Task RegisteringAUserReturnsTheirId()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/users", new { firstName = "Anna", lastName = "Wilson" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<RegisteredUserResponse>();
        Assert.NotEqual(Guid.Empty, created!.Id);
    }

    [Fact]
    public async Task RegisteringTheSameNameTwiceConflicts()
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/users", new { firstName = "Anna", lastName = "Wilson" });
        var second = await client.PostAsJsonAsync("/api/users", new { firstName = "Anna", lastName = "Wilson" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("anna", "wilson")]
    [InlineData("  Anna  ", " Wilson ")]
    [InlineData("ANNA", "WiLsOn")]
    public async Task RegisteringAVariantOfAnExistingNameConflicts(string firstName, string lastName)
    {
        using var factory = new FitnessApiFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/users", new { firstName = "Anna", lastName = "Wilson" });
        var second = await client.PostAsJsonAsync("/api/users", new { firstName, lastName });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Theory]
    [InlineData("", "Wilson")]
    [InlineData("   ", "Wilson")]
    [InlineData("Anna", "")]
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

        await client.PostAsJsonAsync("/api/users", new { firstName = "  Anna ", lastName = "de Vries" });

        var users = await client.GetFromJsonAsync<List<UserResponse>>("/api/users");

        var user = Assert.Single(users!);
        Assert.Equal("Anna", user.FirstName);
        Assert.Equal("de Vries", user.LastName);
    }
}
