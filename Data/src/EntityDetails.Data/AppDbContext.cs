using EntityDetails.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EntityDetails.Data;

/// <summary>
/// The Entity Framework Core database context for the application's persisted entities.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">The options used to configure this context.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// The persisted weather forecasts.
    /// </summary>
    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
}
