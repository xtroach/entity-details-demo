using EntityDetails.Data;
using EntityDetails.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
    /// <exception cref="InvalidOperationException">
    /// The <c>ConnectionStrings:AppDbContext</c> setting is missing.
    /// </exception>
    /// <remarks>
    /// Applies pending EF Core migrations at startup, which is safe while a single instance runs
    /// (Docker Compose, local development). A multi-instance deployment should apply them in a
    /// deploy step before rollout instead.
    /// </remarks>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        // No fallback: a deployment without a connection string fails here, at startup, rather than
        // on its first request.
        // GSS (Kerberos) encryption is turned off: Npgsql tries it by default, but the ASP.NET runtime
        // image has no Kerberos library, so every connection would log a libgssapi_krb5 load error
        // before falling back. Nothing here uses Kerberos; TLS is still negotiated as configured.
        var connectionString = new NpgsqlConnectionStringBuilder(
            builder.Configuration.GetConnectionString("AppDbContext")
                ?? throw new InvalidOperationException(
                    "The connection string 'ConnectionStrings:AppDbContext' is not configured."))
        {
            GssEncryptionMode = GssEncryptionMode.Disable,
        }.ConnectionString;

        // Managed databases occasionally fail over or drop connections; retrying covers those
        // transient errors. The app uses no explicit transactions, which this strategy would require
        // to be wrapped in the strategy's Execute.
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

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

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Database.Migrate();
            if (app.Environment.IsDevelopment())
            {
                SeedDevelopmentData(dbContext);
            }
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

        app.Run();
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
