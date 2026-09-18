using EntityDetails.Api.Mapping;
using EntityDetails.Contracts;
using EntityDetails.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EntityDetails.Api.Controllers;

/// <summary>
/// Provides CRUD access to weather forecast data.
/// </summary>
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly AppDbContext dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherForecastController"/> class.
    /// </summary>
    /// <param name="dbContext">The database context used to read and write forecasts.</param>
    public WeatherForecastController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    /// <summary>
    /// Gets all stored weather forecasts.
    /// </summary>
    /// <returns>Every stored forecast.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WeatherForecastDto>>> Get()
    {
        var forecasts = await dbContext.WeatherForecasts.AsNoTracking().ToListAsync();
        return Ok(forecasts.Select(WeatherForecastMapper.ToDto));
    }

    /// <summary>
    /// Gets a single weather forecast by its identifier.
    /// </summary>
    /// <param name="id">The forecast's unique identifier.</param>
    /// <returns>The matching forecast, or a 404 response if none exists.</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<WeatherForecastDto>> GetById(int id)
    {
        var forecast = await dbContext.WeatherForecasts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        return forecast is null ? NotFound() : Ok(WeatherForecastMapper.ToDto(forecast));
    }

    /// <summary>
    /// Creates a new weather forecast.
    /// </summary>
    /// <param name="request">The forecast to create.</param>
    /// <returns>The created forecast, including its assigned identifier.</returns>
    [HttpPost]
    public async Task<ActionResult<WeatherForecastDto>> Create(WeatherForecastRequest request)
    {
        var forecast = WeatherForecastMapper.ToEntity(request);
        dbContext.WeatherForecasts.Add(forecast);
        await dbContext.SaveChangesAsync();

        var dto = WeatherForecastMapper.ToDto(forecast);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>
    /// Updates an existing weather forecast.
    /// </summary>
    /// <param name="id">The identifier of the forecast to update.</param>
    /// <param name="request">The updated forecast data.</param>
    /// <returns>A 204 response on success, or a 404 response if the forecast does not exist.</returns>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, WeatherForecastRequest request)
    {
        var forecast = await dbContext.WeatherForecasts.FindAsync(id);
        if (forecast is null)
        {
            return NotFound();
        }

        WeatherForecastMapper.Apply(request, forecast);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Deletes a weather forecast.
    /// </summary>
    /// <param name="id">The identifier of the forecast to delete.</param>
    /// <returns>A 204 response on success, or a 404 response if the forecast does not exist.</returns>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var forecast = await dbContext.WeatherForecasts.FindAsync(id);
        if (forecast is null)
        {
            return NotFound();
        }

        dbContext.WeatherForecasts.Remove(forecast);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }
}
