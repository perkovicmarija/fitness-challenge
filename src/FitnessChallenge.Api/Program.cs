using System.Text.Json.Serialization;
using FitnessChallenge.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        // A body carrying an unknown property is rejected. The rule that exactly one measurement
        // may be supplied means nothing if extra fields can be smuggled past it.
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<FitnessDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// The database is disposable and never upgraded in place, so the schema is created on startup
// rather than through migrations. See docs/contract.md.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<FitnessDbContext>().Database.EnsureCreated();
}

app.Run();

// Exposed so the integration tests can host the application through WebApplicationFactory.
public partial class Program;
