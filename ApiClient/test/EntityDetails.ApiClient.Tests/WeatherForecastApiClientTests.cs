using System.Net;
using System.Net.Http.Json;
using EntityDetails.Contracts;

namespace EntityDetails.ApiClient.Tests;

/// <summary>
/// Tests for <see cref="WeatherForecastApiClient"/>.
/// </summary>
public class WeatherForecastApiClientTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsDeserializedForecasts()
    {
        var expected = new List<WeatherForecastDto>
        {
            new(1, DateOnly.FromDateTime(DateTime.Today), 20, 68, "Mild"),
        };
        var client = CreateClient(request =>
        {
            Assert.Equal("weatherforecast", request.RequestUri!.AbsolutePath.Trim('/'));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expected) };
        });

        var forecasts = await client.GetAllAsync();

        Assert.Single(forecasts);
        Assert.Equal("Mild", forecasts[0].Summary);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenApiRespondsNotFound()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        var forecast = await client.GetByIdAsync(42);

        Assert.Null(forecast);
    }

    private static WeatherForecastApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://api.test/"),
        };

        return new WeatherForecastApiClient(httpClient);
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
