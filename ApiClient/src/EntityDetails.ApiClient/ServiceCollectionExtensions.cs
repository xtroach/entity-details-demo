using Microsoft.Extensions.DependencyInjection;

namespace EntityDetails.ApiClient;

/// <summary>
/// Extension methods for registering the API client with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IWeatherForecastApiClient"/> as a typed <see cref="HttpClient"/> pointed
    /// at the given API base address.
    /// </summary>
    /// <param name="services">The service collection to add the client to.</param>
    /// <param name="apiBaseAddress">The base address of the API to call.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddEntityDetailsApiClient(this IServiceCollection services, Uri apiBaseAddress)
    {
        services.AddHttpClient<IWeatherForecastApiClient, WeatherForecastApiClient>(client =>
        {
            client.BaseAddress = apiBaseAddress;
        });

        return services;
    }
}
