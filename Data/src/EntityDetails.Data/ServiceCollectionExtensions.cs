using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace EntityDetails.Data;

/// <summary>
/// Extension methods for registering the data layer with dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The key the data layer's <see cref="NpgsqlDataSource"/> is registered under, so it can't
    /// collide with a data source the host registers for its own use.
    /// </summary>
    private const string DataSourceKey = "EntityDetails.Data";

    /// <summary>
    /// Registers <see cref="AppDbContext"/> against the PostgreSQL database at the given connection
    /// string, with the provider settings the data layer requires.
    /// </summary>
    /// <param name="services">The service collection to add the data layer to.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="configure">
    /// Optionally adjusts <see cref="EntityDetailsDataOptions"/>, e.g. to turn on Microsoft Entra ID
    /// authentication. Without it, the connection string is used as it is.
    /// </param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connectionString"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString"/> is empty or whitespace, or contains a password while
    /// Entra authentication is on.
    /// </exception>
    /// <remarks>
    /// Registration never touches the database. Applying migrations is a separate, explicit step
    /// (<see cref="ServiceProviderExtensions.MigrateEntityDetailsDatabaseAsync"/>), so the host
    /// decides whether it happens at startup or in a deploy step. All contexts share one
    /// <see cref="NpgsqlDataSource"/> (its connection pool and, with Entra authentication, its
    /// access token), which the service provider disposes.
    /// </remarks>
    public static IServiceCollection AddEntityDetailsData(
        this IServiceCollection services, string connectionString, Action<EntityDetailsDataOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var options = new EntityDetailsDataOptions();
        configure?.Invoke(options);
        AppDbContextOptions.Validate(connectionString, options);

        services.AddKeyedSingleton(DataSourceKey, (_, _) => AppDbContextOptions.CreateDataSource(connectionString, options));
        services.AddDbContext<AppDbContext>((serviceProvider, dbContextOptions) => AppDbContextOptions.Configure(
            dbContextOptions, serviceProvider.GetRequiredKeyedService<NpgsqlDataSource>(DataSourceKey)));

        return services;
    }
}
