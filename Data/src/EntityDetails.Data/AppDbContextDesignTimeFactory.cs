using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EntityDetails.Data;

/// <summary>
/// Creates <see cref="AppDbContext"/> instances for EF Core design-time tools such as
/// <c>dotnet ef migrations add</c>.
/// </summary>
/// <remarks>
/// This factory makes the <c>Data</c> project its own startup project for <c>dotnet ef</c>, so the
/// tools need neither the API's startup and configuration nor the EF Core Design package in the
/// API. Adding a migration never opens a connection; the connection string only matters for
/// commands that do, such as <c>dotnet ef database update</c>, which then target the local
/// PostgreSQL container started by <c>docker compose up -d db</c>.
/// </remarks>
public class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <summary>
    /// The connection string of the local Docker Compose database, used by design-time commands.
    /// </summary>
    public const string LocalConnectionString = "Host=localhost;Port=5432;Database=entitydetails;Username=postgres";

    /// <summary>
    /// Creates an <see cref="AppDbContext"/> configured for PostgreSQL against the local Compose
    /// database, with the same provider settings as the runtime registration.
    /// </summary>
    /// <param name="args">Arguments passed by the design-time tools; not used.</param>
    /// <returns>A new <see cref="AppDbContext"/> using the Npgsql provider.</returns>
    /// <remarks>
    /// The data source isn't disposed with the context. Design-time commands are short-lived
    /// processes, so its connection pool ends with them.
    /// </remarks>
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        AppDbContextOptions.Configure(
            options, AppDbContextOptions.CreateDataSource(LocalConnectionString, new EntityDetailsDataOptions()));

        return new AppDbContext(options.Options);
    }
}
