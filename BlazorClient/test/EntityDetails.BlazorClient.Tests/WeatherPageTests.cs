using Bunit;
using EntityDetails.ApiClient;
using EntityDetails.BlazorClient.Pages;
using EntityDetails.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace EntityDetails.BlazorClient.Tests;

/// <summary>
/// Component tests for the <see cref="Weather"/> page.
/// </summary>
public class WeatherPageTests : BunitContext
{
    [Fact]
    public void RendersForecasts_ReturnedByTheApiClient()
    {
        var forecasts = new List<WeatherForecastDto>
        {
            new(1, DateOnly.FromDateTime(DateTime.Today), 20, 68, "Mild"),
        };
        Services.AddSingleton<IWeatherForecastApiClient>(new FakeWeatherForecastApiClient(forecasts));

        var component = Render<Weather>();

        Assert.Contains("Mild", component.Markup);
    }

    [Fact]
    public void ShowsEmptyMessage_WhenNoForecastsExist()
    {
        Services.AddSingleton<IWeatherForecastApiClient>(new FakeWeatherForecastApiClient([]));

        var component = Render<Weather>();

        Assert.Contains("No forecasts are available yet.", component.Markup);
    }

    private sealed class FakeWeatherForecastApiClient(IReadOnlyList<WeatherForecastDto> forecasts) : IWeatherForecastApiClient
    {
        public Task<IReadOnlyList<WeatherForecastDto>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(forecasts);

        public Task<WeatherForecastDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(forecasts.FirstOrDefault(f => f.Id == id));

        public Task<WeatherForecastDto> CreateAsync(WeatherForecastRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(int id, WeatherForecastRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
