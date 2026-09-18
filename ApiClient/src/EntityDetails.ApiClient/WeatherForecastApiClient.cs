using System.Net;
using System.Net.Http.Json;
using EntityDetails.Contracts;

namespace EntityDetails.ApiClient;

/// <summary>
/// HTTP-based implementation of <see cref="IWeatherForecastApiClient"/> that calls the API's
/// <c>weatherforecast</c> endpoints.
/// </summary>
public class WeatherForecastApiClient : IWeatherForecastApiClient
{
    private const string BaseRoute = "weatherforecast";

    private readonly HttpClient httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="WeatherForecastApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured with the API's base address.</param>
    public WeatherForecastApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<WeatherForecastDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var forecasts = await httpClient.GetFromJsonAsync<List<WeatherForecastDto>>(BaseRoute, cancellationToken);
        return forecasts ?? [];
    }

    /// <inheritdoc/>
    public async Task<WeatherForecastDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"{BaseRoute}/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<WeatherForecastDto>(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<WeatherForecastDto> CreateAsync(WeatherForecastRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(BaseRoute, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<WeatherForecastDto>(cancellationToken);
        return created ?? throw new InvalidOperationException("The API did not return the created forecast.");
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(int id, WeatherForecastRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"{BaseRoute}/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"{BaseRoute}/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
