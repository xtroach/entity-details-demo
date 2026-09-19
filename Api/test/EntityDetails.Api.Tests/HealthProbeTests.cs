using System.Net;

namespace EntityDetails.Api.Tests;

/// <summary>
/// Tests for <see cref="HealthProbe"/>, the container health probe behind <c>--probe</c>.
/// </summary>
public class HealthProbeTests
{
    private static readonly Uri LiveUri = new("http://localhost:8080/health/live");

    [Theory]
    [InlineData(null, "http://localhost:8080/health/live")]
    [InlineData("8080", "http://localhost:8080/health/live")]
    [InlineData("5000;5001", "http://localhost:5000/health/live")]
    [InlineData(" 5000 , 5001", "http://localhost:5000/health/live")]
    public void CreateLiveUri_UsesFirstHttpPort_OrDefaultsTo8080(string? httpPorts, string expected)
    {
        Assert.Equal(new Uri(expected), HealthProbe.CreateLiveUri(httpPorts));
    }

    [Fact]
    public async Task RunAsync_ReturnsZero_WhenEndpointIsHealthy()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Equal(0, await HealthProbe.RunAsync(client, LiveUri, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_ReturnsOne_WhenEndpointIsUnhealthy()
    {
        using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        Assert.Equal(1, await HealthProbe.RunAsync(client, LiveUri, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_ReturnsOne_WhenRequestFails()
    {
        using var client = new HttpClient(new StubHandler(_ => throw new HttpRequestException("Connection refused")));

        Assert.Equal(1, await HealthProbe.RunAsync(client, LiveUri, CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_ReturnsOne_WhenRequestTimesOut()
    {
        using var client = new HttpClient(new StubHandler(_ => throw new TaskCanceledException("Timed out")));

        Assert.Equal(1, await HealthProbe.RunAsync(client, LiveUri, CancellationToken.None));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
