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
    private readonly string connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomWebApplicationFactory"/> class.
    /// </summary>
    /// <param name="postgres">The shared PostgreSQL container to create this factory's database in.</param>
    public CustomWebApplicationFactory(PostgresFixture postgres)
    {
        // A unique database per factory instance keeps tests isolated from each other. Migrate()
        // creates it on startup.
        connectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Database = $"entitydetails_tests_{Guid.NewGuid():N}",
        }.ConnectionString;
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development", so Program's dev-only seed data doesn't populate the test database.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:AppDbContext", connectionString);
    }
}
