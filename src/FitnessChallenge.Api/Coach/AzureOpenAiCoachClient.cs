using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace FitnessChallenge.Api.Coach;

internal sealed class AzureOpenAiCoachClient(HttpClient http, IOptions<CoachOptions> options) : ICoachClient
{
    public async Task<string> ReplyAsync(
        string briefing,
        IReadOnlyList<CoachMessage> conversation,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var request = new
        {
            messages = conversation
                .Select(turn => new { role = turn.Role, content = turn.Text })
                .Prepend(new { role = "system", content = briefing }),
            temperature = 0.4,
            max_tokens = 400,
        };

        using var response = await http.PostAsJsonAsync(
            $"openai/deployments/{settings.Deployment}/chat/completions?api-version={settings.ApiVersion}",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {

            throw new HttpRequestException($"Azure OpenAI answered {(int)response.StatusCode}.");
        }

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletion>(cancellationToken);
        var reply = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        return string.IsNullOrEmpty(reply)
            ? throw new HttpRequestException("Azure OpenAI answered without a message.")
            : reply;
    }

    private sealed record ChatCompletion([property: JsonPropertyName("choices")] IReadOnlyList<Choice>? Choices);

    private sealed record Choice([property: JsonPropertyName("message")] ReplyMessage? Message);

    private sealed record ReplyMessage([property: JsonPropertyName("content")] string? Content);
}
