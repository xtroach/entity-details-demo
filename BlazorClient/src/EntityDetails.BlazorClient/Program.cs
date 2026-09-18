using EntityDetails.ApiClient;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace EntityDetails.BlazorClient;

/// <summary>
/// The application entry point.
/// </summary>
public class Program
{
    /// <summary>
    /// Configures and starts the Blazor WebAssembly host.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the process.</param>
    /// <returns>A task that completes when the host stops running.</returns>
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7178";
        builder.Services.AddEntityDetailsApiClient(new Uri(apiBaseUrl));

        await builder.Build().RunAsync();
    }
}
