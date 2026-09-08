using FitnessChallenge.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FitnessChallenge.Api.Tests;

/// <summary>
/// Hosts the API against a private in-memory SQLite database. One factory is one database, so
/// tests never see each other's data and nothing needs cleaning up between runs.
/// </summary>
public sealed class FitnessApiFactory : WebApplicationFactory<Program>
{
    // An in-memory SQLite database exists only while a connection to it is open, so the factory
    // holds one for its whole lifetime.
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<FitnessDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<FitnessDbContext>(options => options.UseSqlite(_connection));
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
