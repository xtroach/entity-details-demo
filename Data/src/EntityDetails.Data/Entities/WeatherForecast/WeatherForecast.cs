namespace EntityDetails.Data.Entities;

/// <summary>
/// A forecasted weather reading for a single day.
/// </summary>
public class WeatherForecast
{
    /// <summary>
    /// The unique identifier of the forecast.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The date the forecast applies to.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// The forecasted temperature, in degrees Celsius.
    /// </summary>
    public int TemperatureC { get; set; }

    /// <summary>
    /// The forecasted temperature, converted to degrees Fahrenheit from <see cref="TemperatureC"/>.
    /// </summary>
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    /// <summary>
    /// A short, human-readable description of the forecasted conditions (e.g. "Mild").
    /// </summary>
    public string? Summary { get; set; }
}
