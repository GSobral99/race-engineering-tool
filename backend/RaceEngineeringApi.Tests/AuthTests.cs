using System.Net;
using RaceEngineeringApi.Tests.Infrastructure;
using Xunit;

namespace RaceEngineeringApi.Tests;

public class AuthTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public AuthTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Sessions_WithoutApiKey_Returns401()
    {
        var response = await _client.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sessions_WithWrongApiKey_Returns401()
    {
        _client.DefaultRequestHeaders.Add("X-Api-Key", "definitely-wrong-key");
        var response = await _client.GetAsync("/api/sessions");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Sessions_WithCorrectApiKey_Returns200()
    {
        _client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFactory.TestApiKey);
        var response = await _client.GetAsync("/api/sessions");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Health_DoesNotRequireApiKey()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }
}