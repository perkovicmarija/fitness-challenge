using System.Text.Json.Serialization;
using FitnessChallenge.Api.Coach;
using FitnessChallenge.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>

        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);

builder.Services.AddOpenApi();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<FitnessDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddScoped<StandingsQuery>();
builder.Services.AddScoped<ChallengeQuery>();

builder.Services.Configure<CoachOptions>(builder.Configuration.GetSection(CoachOptions.SectionName));

builder.Services.AddHttpClient<ICoachClient, AzureOpenAiCoachClient>((provider, http) =>
{
    var coach = provider.GetRequiredService<IOptions<CoachOptions>>().Value;

    if (!coach.IsConfigured)
    {
        return;
    }

    http.BaseAddress = coach.ResourceUri;
    http.DefaultRequestHeaders.Add("api-key", coach.ApiKey!);

    http.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.UseDefaultFiles();
app.UseStaticFiles();

var appShell = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");

app.MapFallback(context =>
{
    if (context.Request.Path.StartsWithSegments("/api") || !File.Exists(appShell))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }

    return context.Response.SendFileAsync(appShell, context.RequestAborted);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FitnessDbContext>();
    db.Database.EnsureCreated();

    if (app.Configuration.GetValue<bool>("Seed"))
    {
        DemoData.Populate(db, app.Services.GetRequiredService<TimeProvider>());
    }
}

app.Run();

public partial class Program;
