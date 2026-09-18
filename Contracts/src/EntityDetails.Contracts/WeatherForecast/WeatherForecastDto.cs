namespace EntityDetails.Contracts;

/// <summary>
/// A weather forecast for a single day, as exposed by the API.
/// </summary>
/// <param name="Id">The unique identifier of the forecast.</param>
/// <param name="Date">The date the forecast applies to.</param>
/// <param name="TemperatureC">The forecasted temperature, in degrees Celsius.</param>
/// <param name="TemperatureF">The forecasted temperature, in degrees Fahrenheit.</param>
/// <param name="Summary">A short, human-readable description of the forecasted conditions.</param>
public record WeatherForecastDto(int Id, DateOnly Date, int TemperatureC, int TemperatureF, string? Summary);
