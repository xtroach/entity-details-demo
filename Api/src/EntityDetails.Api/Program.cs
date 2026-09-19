using EntityDetails.Data;
using EntityDetails.Data.Entities;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace EntityDetails.Api;

/// <summary>
/// The application entry point.
/// </summary>
public class Program
{
    private const string BlazorClientCorsPolicy = "BlazorClient";

    private const string ReadyTag = "ready";

    private const string MigrateArgument = "--migrate";

    /// <summary>
    /// Configures and starts the web application, or runs one of its command-line modes.
    /// </summary>
    /// <param name="args">
    /// The command-line arguments passed to the process. Two modes replace the web application:
    /// <c>--probe</c> (alone) runs <see cref="HealthProbe"/> against the running API, and
    /// <c>--migrate</c> applies pending migrations with the normal configuration and exits. Any
    /// other arguments are configuration, as usual.
    /// </param>
    /// <returns>
    /// The process exit code: 0 after a normal shutdown, a healthy probe or a successful migration;
    /// 1 for an unhealthy probe. A failed migration throws, which exits non-zero.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// The <c>ConnectionStrings:AppDbContext</c> setting is missing.
    /// </exception>
    /// <remarks>
    /// By default the API applies pending migrations at startup, which is safe while a single
    /// instance runs (Docker Compose, local development). A deployment that runs several instances
    /// sets <c>Database:MigrateOnStartup</c> to <see langword="false"/> and runs <c>--migrate</c>
    /// once, as a deploy step before rollout.
    /// </remarks>
    public static async Task<int> Main(string[] args)
    {
        if (args is [HealthProbe.Argument])
        {
            using var client = new HttpClient { Timeout = HealthProbe.Timeout };
            var liveUri = HealthProbe.CreateLiveUri(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"));
            return await HealthProbe.RunAsync(client, liveUri, CancellationToken.None);
        }

        // Removed before the configuration sees the arguments: the command-line configuration
        // provider would otherwise read "--migrate" as a setting.
        var migrateOnly = args.Contains(MigrateArgument);
        var builder = WebApplication.CreateBuilder(args.Where(arg => arg != MigrateArgument).ToArray());

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // No fallback: a deployment without a connection string fails here, at startup, rather than
        // on its first request. The provider and its settings are the data layer's concern.
        var connectionString = builder.Configuration.GetConnectionString("AppDbContext")
            ?? throw new InvalidOperationException(
                "The connection string 'ConnectionStrings:AppDbContext' is not configured.");
        var database = builder.Configuration.GetSection("Database");
        builder.Services.AddEntityDetailsData(connectionString, options =>
        {
            // Opt-in: on Azure, the API signs in to PostgreSQL with its managed identity instead of
            // a password. Everywhere else the connection string is used as it is.
            options.UseEntraAuthentication = database.GetValue<bool>("UseEntraAuthentication");
            options.ManagedIdentityClientId = database["ManagedIdentityClientId"];
        });

        // Liveness runs no checks (the process answers); readiness checks the database. Only
        // checks tagged "ready" run on /health/ready.
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>(tags: [ReadyTag]);

        var blazorClientOrigins = builder.Configuration.GetSection("BlazorClientOrigins").Get<string[]>()
            ?? ["https://localhost:7137", "http://localhost:5286"];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(BlazorClientCorsPolicy, policy => policy
                .WithOrigins(blazorClientOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        await using var app = builder.Build();

        if (migrateOnly)
        {
            await app.Services.MigrateEntityDetailsDatabaseAsync();
            app.Logger.LogInformation("Migrations applied; exiting ({Argument} mode).", MigrateArgument);
            return 0;
        }

        if (database.GetValue("MigrateOnStartup", defaultValue: true))
        {
            await app.Services.MigrateEntityDetailsDatabaseAsync();
        }

        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            SeedDevelopmentData(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseCors(BlazorClientCorsPolicy);

        app.UseAuthorization();

        app.MapControllers();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });

        await app.RunAsync();
        return 0;
    }

    private static void SeedDevelopmentData(AppDbContext dbContext)
    {
        if (dbContext.WeatherForecasts.Any())
        {
            return;
        }

        string[] summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching",
        ];

        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)],
        });

        dbContext.WeatherForecasts.AddRange(forecasts);
        dbContext.SaveChanges();
    }
}
