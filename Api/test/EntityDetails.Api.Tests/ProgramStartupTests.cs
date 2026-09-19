using EntityDetails.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EntityDetails.Api.Tests;

/// <summary>
/// Tests for the database setup <see cref="Program"/> performs at startup.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ProgramStartupTests(PostgresFixture postgres)
{
    [Fact]
    public void Startup_AppliesAllMigrations()
    {
        using var factory = new CustomWebApplicationFactory(postgres);
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Contains(dbContext.Database.GetAppliedMigrations(), id => id.EndsWith("_InitialCreate"));
        Assert.Empty(dbContext.Database.GetPendingMigrations());
    }

    [Fact]
    public async Task Startup_DoesNotMigrate_WhenMigrateOnStartupIsFalse()
    {
        using var factory = new CustomWebApplicationFactory(
            postgres, new Dictionary<string, string?> { ["Database:MigrateOnStartup"] = "false" });
        using var client = factory.CreateClient();

        // Migrating would have created the factory's database; without it, there's nothing to reach.
        await using var services = BuildDataServices(factory.ConnectionString);
        await using var scope = services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.CanConnectAsync());
    }

    [Fact]
    public async Task Main_MigrateMode_AppliesMigrationsAndExitsWithZero()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
        {
            Database = $"entitydetails_tests_{Guid.NewGuid():N}",
        }.ConnectionString;

        // Configuration comes in as command-line arguments, as in the container job; that avoids
        // changing process-wide environment variables in a test.
        var exitCode = await Program.Main(
            ["--migrate", $"--ConnectionStrings:AppDbContext={connectionString}", "--environment=Testing"]);

        Assert.Equal(0, exitCode);
        await using var services = BuildDataServices(connectionString);
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(await dbContext.Database.GetAppliedMigrationsAsync(), id => id.EndsWith("_InitialCreate"));
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public void Startup_Throws_WhenConnectionStringIsMissing()
    {
        // "Testing" loads only appsettings.json, which deliberately has no connection string.
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("ConnectionStrings:AppDbContext", exception.Message);
    }

    private static ServiceProvider BuildDataServices(string connectionString) =>
        new ServiceCollection().AddEntityDetailsData(connectionString).BuildServiceProvider();
}
