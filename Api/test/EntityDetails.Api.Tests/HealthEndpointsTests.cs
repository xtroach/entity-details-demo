using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EntityDetails.Api.Tests;

/// <summary>
/// Integration tests for the API's liveness and readiness endpoints.
/// </summary>
[Collection(PostgresCollection.Name)]
public class HealthEndpointsTests(PostgresFixture postgres)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_ReturnsHealthy_WhenDatabaseIsReachable(string path)
    {
        using var factory = new CustomWebApplicationFactory(postgres);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Endpoints_ReportReadyUnhealthy_ButLive_WhenDatabaseIsUnreachable()
    {
        // Nothing listens on port 1. Startup migration is off, so the API starts without a database.
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Testing")
            .UseSetting("ConnectionStrings:AppDbContext", "Host=127.0.0.1;Port=1;Database=unreachable;Username=test;Timeout=2")
            .UseSetting("Database:MigrateOnStartup", "false"));
        using var client = factory.CreateClient();

        var ready = await client.GetAsync("/health/ready");
        var live = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.Equal("Unhealthy", await ready.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }
}
