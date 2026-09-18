using EntityDetails.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntityDetails.Data.Tests;

/// <summary>
/// Tests for <see cref="AppDbContext"/>.
/// </summary>
public class AppDbContextTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"EntityDetailsDataTests-{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task WeatherForecasts_PersistsAndReturnsAddedEntity()
    {
        using var dbContext = CreateContext();
        var forecast = new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Today),
            TemperatureC = 25,
            Summary = "Warm",
        };

        dbContext.WeatherForecasts.Add(forecast);
        await dbContext.SaveChangesAsync();

        var stored = await dbContext.WeatherForecasts.SingleAsync();
        Assert.Equal(forecast.Id, stored.Id);
        Assert.Equal("Warm", stored.Summary);
    }

    [Fact]
    public void WeatherForecast_TemperatureF_IsConvertedFromCelsius()
    {
        var forecast = new WeatherForecast { TemperatureC = 0 };

        Assert.Equal(32, forecast.TemperatureF);
    }
}
