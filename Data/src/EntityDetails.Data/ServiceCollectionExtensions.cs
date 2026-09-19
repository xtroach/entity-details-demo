using Microsoft.Extensions.DependencyInjection;

namespace EntityDetails.Data;

/// <summary>
/// Extension methods for registering the data layer with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AppDbContext"/> against the PostgreSQL database at the given connection
    /// string, with the provider settings the data layer requires.
    /// </summary>
    /// <param name="services">The service collection to add the data layer to.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is empty or whitespace.</exception>
    /// <remarks>
    /// Registration never touches the database. Applying migrations is a separate, explicit step
    /// (<see cref="ServiceProviderExtensions.MigrateEntityDetailsDatabaseAsync"/>), so the host
    /// decides whether it happens at startup or in a deploy step.
    /// </remarks>
    public static IServiceCollection AddEntityDetailsData(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<AppDbContext>(options => AppDbContextOptions.Configure(options, connectionString));

        return services;
    }
}
