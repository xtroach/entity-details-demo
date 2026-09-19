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
    /// How often a fresh Entra access token is fetched. Tokens are valid for 60–90 minutes, so this
    /// always keeps a valid one in hand.
    /// </summary>
    private static readonly TimeSpan TokenRefreshInterval = TimeSpan.FromMinutes(30);

    /// <summary>
    /// How soon a failed token request is retried.
    /// </summary>
    private static readonly TimeSpan TokenRetryInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Checks that <paramref name="connectionString"/> and <paramref name="options"/> can be used
    /// together, so a misconfiguration fails at registration rather than on the first connection.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="options">The data layer options.</param>
    /// <exception cref="ArgumentException">
    /// Entra authentication is on and the connection string also contains a password.
    /// </exception>
    public static void Validate(string connectionString, EntityDetailsDataOptions options)
    {
        if (options.UseEntraAuthentication
            && !string.IsNullOrEmpty(new NpgsqlConnectionStringBuilder(connectionString).Password))
        {
            throw new ArgumentException(
                "The connection string contains a password, but Entra authentication is enabled. "
                    + "Remove the password: the access token is used instead.",
                nameof(connectionString));
        }
    }

    /// <summary>
    /// Builds the data source every <see cref="AppDbContext"/> connects through.
    /// </summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <param name="options">The data layer options.</param>
    /// <returns>A data source for the connection string, with the data layer's settings applied.</returns>
    /// <remarks>
    /// GSS (Kerberos) encryption is turned off: Npgsql tries it by default, but the ASP.NET runtime
    /// image has no Kerberos library, so every connection would log a libgssapi_krb5 load error
    /// before falling back. Nothing here uses Kerberos; TLS is still negotiated as configured. With
    /// Entra authentication on, the password is an access token that Npgsql refreshes in the
    /// background.
    /// </remarks>
    public static NpgsqlDataSource CreateDataSource(string connectionString, EntityDetailsDataOptions options)
    {
        var builder = new NpgsqlDataSourceBuilder(connectionString);
        builder.ConnectionStringBuilder.GssEncryptionMode = GssEncryptionMode.Disable;

        if (options.UseEntraAuthentication)
        {
            var credential = EntraAuthentication.CreateCredential(options);
            builder.UsePeriodicPasswordProvider(
                (_, cancellationToken) => EntraAuthentication.GetAccessTokenAsync(credential, cancellationToken),
                TokenRefreshInterval,
                TokenRetryInterval);
        }

        return builder.Build();
    }

    /// <summary>
    /// Configures <paramref name="options"/> to use PostgreSQL through Npgsql and
    /// <paramref name="dataSource"/>.
    /// </summary>
    /// <param name="options">The options builder to configure.</param>
    /// <param name="dataSource">The data source to connect through.</param>
    /// <remarks>
    /// Retrying covers the transient errors managed databases produce when they fail over or drop
    /// connections. The app uses no explicit transactions, which this strategy would require to be
    /// wrapped in the strategy's <c>Execute</c>.
    /// </remarks>
    public static void Configure(DbContextOptionsBuilder options, NpgsqlDataSource dataSource)
    {
        options.UseNpgsql(dataSource, npgsql => npgsql.EnableRetryOnFailure());
    }
}
