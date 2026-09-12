using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using RaceEngineeringApi.Tests.Infrastructure;
using Xunit;

namespace RaceEngineeringApi.Tests;

public class ImportEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ImportEndpointTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFactory.TestApiKey);
    }

    [Fact]
    public async Task ImportCsv_ThenGetSession_ReturnsMatchingData()
    {
        const string csv =
            "Driver,StintNumber,Compound,LapNumber,LapTimeSeconds,TyreLife,PredictedLapTimeSeconds,Team\n" +
            "VER,1,SOFT,1,92.451,1,,Red Bull Racing\n" +
            "VER,1,SOFT,2,91.802,2,,Red Bull Racing\n";

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        form.Add(fileContent, "file", "test.csv");
        form.Add(new StringContent("Test Session"), "sessionName");
        form.Add(new StringContent("unit-test"), "source");

        var importResponse = await _client.PostAsync("/api/sessions/import", form);
        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var imported = await importResponse.Content.ReadFromJsonAsync<ImportResult>();
        Assert.NotNull(imported);

        var getResponse = await _client.GetAsync($"/api/sessions/{imported!.Id}");
        getResponse.EnsureSuccessStatusCode();

        var detail = await getResponse.Content.ReadFromJsonAsync<SessionDetailResult>();
        Assert.NotNull(detail);
        Assert.Equal("Test Session", detail!.Name);
        Assert.Single(detail.Stints);
        Assert.Equal(2, detail.Stints[0].Laps.Count);
        Assert.Equal("Red Bull Racing", detail.Stints[0].Team);
    }

    [Fact]
    public async Task GetSession_ForUnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/sessions/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record ImportResult(int Id, string Name);
    private record LapResult(int Id, int LapNumber);
    private record StintResult(int Id, string Driver, string? Team, List<LapResult> Laps);
    private record SessionDetailResult(int Id, string Name, List<StintResult> Stints);
}