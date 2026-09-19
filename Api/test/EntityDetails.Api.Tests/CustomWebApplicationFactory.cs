using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace EntityDetails.Api.Tests;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that points the API at a new, empty database
/// in the shared PostgreSQL test container. The API's own startup applies the migrations to it, so
/// integration tests run against the real provider and schema.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomWebApplicationFactory"/> class.
    /// </summary>
    /// <param name="postgres">The shared PostgreSQL container to create this factory's database in.</param>
    /// <param name="settings">Extra configuration settings for the API, e.g. <c>Database:MigrateOnStartup</c>.</param>
    public CustomWebApplicationFactory(PostgresFixture postgres, IReadOnlyDictionary<string, string?>? settings = null)
    {
        // A unique database per factory instance keeps tests isolated from each other. Migrate()
        // creates it on startup.
        ConnectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Database = $"entitydetails_tests_{Guid.NewGuid():N}",
        }.ConnectionString;
        this.settings = settings ?? new Dictionary<string, string?>();
    }

    /// <summary>
    /// The connection string of this factory's own database.
    /// </summary>
    public string ConnectionString { get; }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development", so Program's dev-only seed data doesn't populate the test database.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDbContext", ConnectionString);
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }
    }
}
