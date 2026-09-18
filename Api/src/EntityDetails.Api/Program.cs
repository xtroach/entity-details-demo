using EntityDetails.Data;
using EntityDetails.Data.Entities;
using Microsoft.EntityFrameworkCore;

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
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext")
                ?? "Data Source=entitydetails.db"));

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
            dbContext.Database.EnsureCreated();
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
