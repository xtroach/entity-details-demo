using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace EntityDetails.Data.Tests;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions.AddEntityDetailsData"/>. They only inspect the
/// registered context's configuration, so no database is needed.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private const string ConnectionString = "Host=localhost;Database=entitydetails_unit;Username=postgres";

    [Fact]
    public void AddEntityDetailsData_RegistersContextWithNpgsqlProvider()
    {
        using var services = BuildServices();
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", dbContext.Database.ProviderName);
    }

    [Fact]
    public void AddEntityDetailsData_DisablesGssEncryption()
    {
        using var services = BuildServices();
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var connectionString = new NpgsqlConnectionStringBuilder(dbContext.Database.GetConnectionString());
        Assert.Equal(GssEncryptionMode.Disable, connectionString.GssEncryptionMode);
        Assert.Equal("entitydetails_unit", connectionString.Database);
    }

    [Fact]
    public void AddEntityDetailsData_RetriesTransientFailures()
    {
        using var services = BuildServices();
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.IsType<NpgsqlRetryingExecutionStrategy>(dbContext.Database.CreateExecutionStrategy());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddEntityDetailsData_Throws_WhenConnectionStringIsEmptyOrWhitespace(string connectionString)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddEntityDetailsData(connectionString));
    }

    [Fact]
    public void AddEntityDetailsData_Throws_WhenConnectionStringIsNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => services.AddEntityDetailsData(null!));
    }

    [Fact]
    public void AddEntityDetailsData_Throws_WhenEntraAuthenticationIsCombinedWithPassword()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() => services.AddEntityDetailsData(
            ConnectionString + ";Password=secret", options => options.UseEntraAuthentication = true));
        Assert.Contains("password", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddEntityDetailsData_WithEntraAuthentication_RegistersNpgsqlContextWithoutPassword()
    {
        using var services = new ServiceCollection()
            .AddEntityDetailsData(ConnectionString, options =>
            {
                options.UseEntraAuthentication = true;
                options.TokenCredential = new EntraAuthenticationTests.FakeTokenCredential();
            })
            .BuildServiceProvider();
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var connectionString = new NpgsqlConnectionStringBuilder(dbContext.Database.GetConnectionString());
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", dbContext.Database.ProviderName);
        Assert.Null(connectionString.Password);
        Assert.Equal(GssEncryptionMode.Disable, connectionString.GssEncryptionMode);
    }

    private static ServiceProvider BuildServices() =>
        new ServiceCollection().AddEntityDetailsData(ConnectionString).BuildServiceProvider();
}
