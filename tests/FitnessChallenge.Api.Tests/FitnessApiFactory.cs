using FitnessChallenge.Api.Coach;
using FitnessChallenge.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FitnessChallenge.Api.Tests;

public sealed class FitnessApiFactory : WebApplicationFactory<Program>
{

    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public DateTimeOffset? Now { get; init; }

    public bool CoachConfigured { get; init; }

    public StubCoachClient Coach { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        if (CoachConfigured)
        {
            builder.UseSetting("Coach:Endpoint", "https://stub.openai.azure.com/");
            builder.UseSetting("Coach:ApiKey", "stub-key");
            builder.UseSetting("Coach:Deployment", "stub-deployment");
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FitnessDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<FitnessDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<ICoachClient>();
            services.AddSingleton<ICoachClient>(Coach);

            if (Now is not null)
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now.Value));
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public sealed class StubCoachClient : ICoachClient
{

    public Func<string> Reply { get; set; } = () => "Run 3.4 km this week and you pass rank 3.";

    public string? LastBriefing { get; private set; }

    public IReadOnlyList<CoachMessage> LastConversation { get; private set; } = [];

    public Task<string> ReplyAsync(
        string briefing, IReadOnlyList<CoachMessage> conversation, CancellationToken cancellationToken)
    {
        LastBriefing = briefing;
        LastConversation = conversation;

        return Task.FromResult(Reply());
    }
}
