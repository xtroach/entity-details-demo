using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EntityDetails.Data;

/// <summary>
/// Extension methods for maintaining the database of a built service provider.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Applies all pending EF Core migrations to the database registered by
    /// <see cref="ServiceCollectionExtensions.AddEntityDetailsData"/>, creating the database if it
    /// doesn't exist.
    /// </summary>
    /// <param name="services">The application's root service provider.</param>
    /// <param name="cancellationToken">A token to cancel the migration.</param>
    /// <returns>A task that completes when every migration has been applied.</returns>
    /// <remarks>
    /// The data layer provides how to migrate; the host decides when. Calling this at startup is
    /// safe while a single instance runs. A multi-instance deployment should apply migrations in a
    /// deploy step before rollout instead.
    /// </remarks>
    public static async Task MigrateEntityDetailsDatabaseAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
