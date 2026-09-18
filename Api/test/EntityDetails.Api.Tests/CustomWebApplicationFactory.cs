using EntityDetails.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntityDetails.Api.Tests;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that swaps the real SQLite database for an
/// isolated in-memory one, so integration tests don't touch the file system.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Computed once per factory instance: AddDbContext's default optionsLifetime is Scoped,
        // so the configure lambda below runs again for every DI scope (i.e. every HTTP request).
        // A Guid generated inside the lambda would give each request its own isolated database.
        var databaseName = $"EntityDetailsTests-{Guid.NewGuid()}";

        // Not "Development", so Program's dev-only seed data doesn't populate the test database.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // AddDbContext registers the SQLite configuration as an additive
            // IDbContextOptionsConfiguration<AppDbContext> entry; both descriptors must be
            // removed or the SQLite and in-memory providers end up registered together.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
