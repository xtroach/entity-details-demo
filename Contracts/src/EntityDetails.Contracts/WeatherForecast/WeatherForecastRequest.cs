namespace EntityDetails.Contracts;

/// <summary>
/// The data needed to create or update a weather forecast.
/// </summary>
/// <param name="Date">The date the forecast applies to.</param>
/// <param name="TemperatureC">The forecasted temperature, in degrees Celsius.</param>
/// <param name="Summary">A short, human-readable description of the forecasted conditions.</param>
public record WeatherForecastRequest(DateOnly Date, int TemperatureC, string? Summary);
