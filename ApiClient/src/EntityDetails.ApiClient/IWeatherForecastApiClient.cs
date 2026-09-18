using EntityDetails.Contracts;

namespace EntityDetails.ApiClient;

/// <summary>
/// Retrieves and manages weather forecasts exposed by the API.
/// </summary>
public interface IWeatherForecastApiClient
{
    /// <summary>
    /// Gets all weather forecasts.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>Every forecast known to the API.</returns>
    Task<IReadOnlyList<WeatherForecastDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single weather forecast by its identifier.
    /// </summary>
    /// <param name="id">The forecast's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The matching forecast, or <see langword="null"/> if none exists.</returns>
    Task<WeatherForecastDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new weather forecast.
    /// </summary>
    /// <param name="request">The forecast to create.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created forecast, including its assigned identifier.</returns>
    Task<WeatherForecastDto> CreateAsync(WeatherForecastRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing weather forecast.
    /// </summary>
    /// <param name="id">The identifier of the forecast to update.</param>
    /// <param name="request">The updated forecast data.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes when the update has been applied.</returns>
    Task UpdateAsync(int id, WeatherForecastRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a weather forecast.
    /// </summary>
    /// <param name="id">The identifier of the forecast to delete.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>A task that completes when the forecast has been deleted.</returns>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
