using EntityDetails.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
    public void Startup_Throws_WhenConnectionStringIsMissing()
    {
        // "Testing" loads only appsettings.json, which deliberately has no connection string.
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());
        Assert.Contains("ConnectionStrings:AppDbContext", exception.Message);
    }
}
