using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RaceEngineeringApi.Data;
using RaceEngineeringApi.Services;
using Xunit;

namespace RaceEngineeringApi.Tests;

public class CsvImportServiceTests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task ImportAsync_GroupsLapsByDriverAndStintNumber()
    {
        const string csv =
            "Driver,StintNumber,Compound,LapNumber,LapTimeSeconds,TyreLife,PredictedLapTimeSeconds,Team\n" +
            "VER,1,SOFT,1,92.451,1,,Red Bull Racing\n" +
            "VER,1,SOFT,2,91.802,2,,Red Bull Racing\n" +
            "VER,2,MEDIUM,3,93.100,1,,Red Bull Racing\n" +
            "HAM,1,MEDIUM,1,93.201,1,,Mercedes\n";

        using var db = CreateInMemoryDb();
        var importer = new CsvImportService(db);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var session = await importer.ImportAsync(stream, "Test", "unit-test");

        // 3 distinct (Driver, StintNumber) groups: VER/1, VER/2, HAM/1
        Assert.Equal(3, session.Stints.Count);

        var verStint1 = session.Stints.Single(s => s.Driver == "VER" && s.StintNumber == 1);
        Assert.Equal(2, verStint1.Laps.Count);
        Assert.Equal("Red Bull Racing", verStint1.Team);
    }

    [Fact]
    public async Task ImportAsync_SortsLapsWithinAStintByLapNumber()
    {
        const string csv =
            "Driver,StintNumber,Compound,LapNumber,LapTimeSeconds,TyreLife\n" +
            "VER,1,SOFT,2,91.802,2\n" +
            "VER,1,SOFT,1,92.451,1\n";

        using var db = CreateInMemoryDb();
        var importer = new CsvImportService(db);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var session = await importer.ImportAsync(stream, "Test", "unit-test");

        var laps = session.Stints.Single().Laps;
        Assert.Equal(1, laps[0].LapNumber);
        Assert.Equal(2, laps[1].LapNumber);
    }

    [Fact]
    public async Task ImportAsync_WithoutTeamColumn_LeavesTeamNull()
    {
        const string csv =
            "Driver,StintNumber,Compound,LapNumber,LapTimeSeconds,TyreLife\n" +
            "HAM,1,MEDIUM,1,93.201,1\n";

        using var db = CreateInMemoryDb();
        var importer = new CsvImportService(db);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var session = await importer.ImportAsync(stream, "Test", "unit-test");

        Assert.Null(session.Stints.Single().Team);
    }

    [Fact]
    public async Task ImportAsync_WithoutPredictedLapTime_LeavesItNull()
    {
        const string csv =
            "Driver,StintNumber,Compound,LapNumber,LapTimeSeconds,TyreLife\n" +
            "HAM,1,MEDIUM,1,93.201,1\n";

        using var db = CreateInMemoryDb();
        var importer = new CsvImportService(db);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var session = await importer.ImportAsync(stream, "Test", "unit-test");

        Assert.Null(session.Stints.Single().Laps.Single().PredictedLapTimeSeconds);
    }
}