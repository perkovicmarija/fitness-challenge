using System.Net.Http.Json;
using FitnessChallenge.Api.DTO;

namespace FitnessChallenge.Api.Tests;

internal static class TestApi
{
    public static async Task<Guid> RegisterAsync(this HttpClient client, string firstName, string lastName)
    {
        var response = await client.PostAsJsonAsync("/api/users", new { firstName, lastName });
        await EnsureCreatedAsync(response);

        var created = await response.Content.ReadFromJsonAsync<RegisteredUserResponse>();
        return created!.Id;
    }

    public static async Task RunAsync(this HttpClient client, Guid userId, string datetime, decimal kilometres)
    {
        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime,
            sport = "running",
            distance = kilometres,
        });

        await EnsureCreatedAsync(response);
    }

    public static async Task CycleAsync(this HttpClient client, Guid userId, string datetime, decimal kilometres)
    {
        var response = await client.PostAsJsonAsync("/api/activities", new
        {
            userId,
            datetime,
            sport = "cycling",
            distance = kilometres,
        });

        await EnsureCreatedAsync(response);
    }

    public static async Task StepsAsync(this HttpClient client, Guid userId, string datetime, int steps)
    {
        var response = await client.PostAsJsonAsync("/api/activities", new { userId, datetime, steps });
        await EnsureCreatedAsync(response);
    }

    private static async Task EnsureCreatedAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }
}
