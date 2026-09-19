namespace EntityDetails.Api;

/// <summary>
/// The API's built-in container health probe (<c>dotnet EntityDetails.Api.dll --probe</c>), used by
/// the Dockerfile's <c>HEALTHCHECK</c>.
/// </summary>
/// <remarks>
/// The ASP.NET runtime image has neither <c>curl</c> nor <c>wget</c>. Probing through the API's own
/// executable keeps extra tools, and their attack surface, out of the runtime image, and still works
/// on chiseled or distroless images.
/// </remarks>
internal static class HealthProbe
{
    /// <summary>
    /// The command-line argument that runs the probe instead of the web application.
    /// </summary>
    public const string Argument = "--probe";

    /// <summary>
    /// How long the probe waits for a response before reporting the API as unhealthy.
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Builds the liveness endpoint's address from the ports the API listens on.
    /// </summary>
    /// <param name="httpPorts">
    /// The value of <c>ASPNETCORE_HTTP_PORTS</c> (e.g. <c>"8080"</c> or <c>"8080;8081"</c>), or
    /// <see langword="null"/> when it isn't set.
    /// </param>
    /// <returns>The <c>/health/live</c> address on the first HTTP port, defaulting to 8080.</returns>
    public static Uri CreateLiveUri(string? httpPorts)
    {
        var port = httpPorts?
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? "8080";

        return new Uri($"http://localhost:{port}/health/live");
    }

    /// <summary>
    /// Requests <paramref name="uri"/> and converts the result into a process exit code.
    /// </summary>
    /// <param name="client">The client that sends the request.</param>
    /// <param name="uri">The health endpoint to request.</param>
    /// <param name="cancellationToken">A token that cancels the request.</param>
    /// <returns>0 when the endpoint answers with a success status code; otherwise 1.</returns>
    /// <remarks>
    /// Any failure, including a refused connection or a timeout, counts as unhealthy rather than
    /// escaping as an exception, so Docker always gets a clean exit code.
    /// </remarks>
    public static async Task<int> RunAsync(HttpClient client, Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(uri, cancellationToken);
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
