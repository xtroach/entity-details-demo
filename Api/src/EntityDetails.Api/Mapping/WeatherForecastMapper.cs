using EntityDetails.Contracts;
using EntityDetails.Data.Entities;

namespace EntityDetails.Api.Mapping;

/// <summary>
/// Maps between the <see cref="WeatherForecast"/> persistence entity and its wire contract types.
/// </summary>
internal static class WeatherForecastMapper
{
    /// <summary>
    /// Maps a persisted forecast to its wire representation.
    /// </summary>
    /// <param name="entity">The persisted forecast.</param>
    /// <returns>The corresponding <see cref="WeatherForecastDto"/>.</returns>
    public static WeatherForecastDto ToDto(WeatherForecast entity) =>
        new(entity.Id, entity.Date, entity.TemperatureC, entity.TemperatureF, entity.Summary);

    /// <summary>
    /// Creates a new, unsaved persistence entity from a create request.
    /// </summary>
    /// <param name="request">The request data.</param>
    /// <returns>A new <see cref="WeatherForecast"/> populated from <paramref name="request"/>.</returns>
    public static WeatherForecast ToEntity(WeatherForecastRequest request) =>
        new()
        {
            Date = request.Date,
            TemperatureC = request.TemperatureC,
            Summary = request.Summary,
        };

    /// <summary>
    /// Applies update request data onto an existing tracked entity.
    /// </summary>
    /// <param name="request">The updated data.</param>
    /// <param name="entity">The tracked entity to update.</param>
    public static void Apply(WeatherForecastRequest request, WeatherForecast entity)
    {
        entity.Date = request.Date;
        entity.TemperatureC = request.TemperatureC;
        entity.Summary = request.Summary;
    }
}
