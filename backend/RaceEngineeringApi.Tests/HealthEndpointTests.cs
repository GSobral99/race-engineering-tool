using System.Net.Http.Json;
using RaceEngineeringApi.Tests.Infrastructure;
using Xunit;

namespace RaceEngineeringApi.Tests;

public class HealthEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("ok", body!.Status);
    }

    private record HealthResponse(string Status);
}