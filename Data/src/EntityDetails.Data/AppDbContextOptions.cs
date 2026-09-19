using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EntityDetails.Data;

/// <summary>
/// The single place <see cref="AppDbContext"/>'s database provider is configured, shared by the
/// runtime registration (<see cref="ServiceCollectionExtensions.AddEntityDetailsData"/>) and the
/// design-time tools (<see cref="AppDbContextDesignTimeFactory"/>).
/// </summary>
/// <remarks>
/// The provider belongs here, next to the migrations, because the two are one decision: the
/// migrations in <c>Migrations/</c> are PostgreSQL-specific and only apply to a context configured
/// for Npgsql. Hosts pass a connection string and never name the provider.
/// </remarks>
internal static class AppDbContextOptions
{
    /// <summary>
    /// Configures <paramref name="options"/> to use PostgreSQL through Npgsql.
    /// </summary>
    /// <param name="options">The options builder to configure.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <remarks>
    /// GSS (Kerberos) encryption is turned off: Npgsql tries it by default, but the ASP.NET runtime
    /// image has no Kerberos library, so every connection would log a libgssapi_krb5 load error
    /// before falling back. Nothing here uses Kerberos; TLS is still negotiated as configured.
    /// Retrying covers the transient errors managed databases produce when they fail over or drop
    /// connections. The app uses no explicit transactions, which this strategy would require to be
    /// wrapped in the strategy's <c>Execute</c>.
    /// </remarks>
    public static void Configure(DbContextOptionsBuilder options, string connectionString)
    {
        var npgsqlConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
        {
            GssEncryptionMode = GssEncryptionMode.Disable,
        }.ConnectionString;

        options.UseNpgsql(npgsqlConnectionString, npgsql => npgsql.EnableRetryOnFailure());
    }
}
