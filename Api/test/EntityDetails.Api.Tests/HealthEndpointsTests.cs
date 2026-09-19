using System.Net;

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
}
