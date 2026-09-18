using System.Net;
using System.Net.Http.Json;
using EntityDetails.Contracts;

namespace EntityDetails.Api.Tests;

/// <summary>
/// Integration tests for the weather forecast CRUD endpoints.
/// </summary>
public class WeatherForecastControllerTests
{
    [Fact]
    public async Task Get_ReturnsEmptyList_WhenNoForecastsExist()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var forecasts = await client.GetFromJsonAsync<List<WeatherForecastDto>>("/weatherforecast");

        Assert.NotNull(forecasts);
        Assert.Empty(forecasts);
    }

    [Fact]
    public async Task Create_ThenGetById_ReturnsTheCreatedForecast()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new WeatherForecastRequest(DateOnly.FromDateTime(DateTime.Today), 21, "Mild");

        var createResponse = await client.PostAsJsonAsync("/weatherforecast", request);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<WeatherForecastDto>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);

        var getResponse = await client.GetAsync($"/weatherforecast/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenForecastDoesNotExist()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/weatherforecast/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesForecast_WhenItExists()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new WeatherForecastRequest(DateOnly.FromDateTime(DateTime.Today), 10, "Cool");
        var createResponse = await client.PostAsJsonAsync("/weatherforecast", request);
        var created = await createResponse.Content.ReadFromJsonAsync<WeatherForecastDto>();

        var deleteResponse = await client.DeleteAsync($"/weatherforecast/{created!.Id}");
        var getResponse = await client.GetAsync($"/weatherforecast/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
