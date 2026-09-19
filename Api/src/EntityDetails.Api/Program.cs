using EntityDetails.Data;
using EntityDetails.Data.Entities;

namespace EntityDetails.Api;

/// <summary>
/// The application entry point.
/// </summary>
public class Program
{
    private const string BlazorClientCorsPolicy = "BlazorClient";

    /// <summary>
    /// Configures and starts the web application.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the process.</param>
    /// <returns>A task that completes when the application shuts down.</returns>
    /// <exception cref="InvalidOperationException">
    /// The <c>ConnectionStrings:AppDbContext</c> setting is missing.
    /// </exception>
    /// <remarks>
    /// Applies pending EF Core migrations at startup, which is safe while a single instance runs
    /// (Docker Compose, local development). A multi-instance deployment should apply them in a
    /// deploy step before rollout instead.
    /// </remarks>
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // No fallback: a deployment without a connection string fails here, at startup, rather than
        // on its first request. The provider and its settings are the data layer's concern.
        var connectionString = builder.Configuration.GetConnectionString("AppDbContext")
            ?? throw new InvalidOperationException(
                "The connection string 'ConnectionStrings:AppDbContext' is not configured.");
        builder.Services.AddEntityDetailsData(connectionString);

        var blazorClientOrigins = builder.Configuration.GetSection("BlazorClientOrigins").Get<string[]>()
            ?? ["https://localhost:7137", "http://localhost:5286"];
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(BlazorClientCorsPolicy, policy => policy
                .WithOrigins(blazorClientOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
        });

        var app = builder.Build();

        await app.Services.MigrateEntityDetailsDatabaseAsync();
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

        await app.RunAsync();
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
