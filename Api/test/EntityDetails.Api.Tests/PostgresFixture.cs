using Testcontainers.PostgreSql;

namespace EntityDetails.Api.Tests;

/// <summary>
/// Starts one PostgreSQL container that every test in the <see cref="PostgresCollection"/> shares.
/// Each <see cref="CustomWebApplicationFactory"/> creates its own database in it, so tests stay
/// isolated without paying for a container per test.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    // The same image and digest as the db service in docker-compose.yml. Dependabot updates the
    // Compose file but not this constant, so keep the two in sync.
    private const string Image =
        "postgres:18-alpine@sha256:6c538e7206ea40ff740ef27883529390a690b6ead6ba96b44c67a9f7c638e8fd";

    private readonly PostgreSqlContainer container = new PostgreSqlBuilder(Image).Build();

    /// <summary>
    /// The connection string of the running container's default database.
    /// </summary>
    public string ConnectionString => container.GetConnectionString();

    /// <inheritdoc/>
    public Task InitializeAsync() => container.StartAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => container.DisposeAsync().AsTask();
}

/// <summary>
/// The xUnit collection whose test classes share one <see cref="PostgresFixture"/>.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    /// <summary>
    /// The collection name used in <see cref="CollectionAttribute"/> on test classes.
    /// </summary>
    public const string Name = "Postgres";
}
