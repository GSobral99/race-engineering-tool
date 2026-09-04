using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using RaceEngineeringApi.Data;
using RaceEngineeringApi.Models;

namespace RaceEngineeringApi.Services;

/// <summary>
/// Expected CSV columns (matches the export format used by ac-lap-coach and
/// the pit-stop-predictor project): Driver, StintNumber, Compound, LapNumber,
/// LapTimeSeconds, TyreLife, PredictedLapTimeSeconds (optional).
/// </summary>
public class LapRow
{
    public required string Driver { get; set; }
    public int StintNumber { get; set; }
    public required string Compound { get; set; }
    public int LapNumber { get; set; }
    public double LapTimeSeconds { get; set; }
    public int TyreLife { get; set; }

    [Optional]
    public double? PredictedLapTimeSeconds { get; set; }
}

public class CsvImportService
{
    private readonly AppDbContext _db;

    public CsvImportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Session> ImportAsync(Stream csvStream, string sessionName, string source)
    {
        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var rows = csv.GetRecords<LapRow>().ToList();

        var session = new Session { Name = sessionName, Source = source };

        // Group rows into stints per (Driver, StintNumber) so laps land under
        // the right stint even if the CSV isn't pre-sorted.
        var stintGroups = rows.GroupBy(r => (r.Driver, r.StintNumber));

        foreach (var group in stintGroups)
        {
            var first = group.First();
            var stint = new Stint
            {
                Driver = first.Driver,
                StintNumber = first.StintNumber,
                Compound = first.Compound,
            };

            foreach (var row in group.OrderBy(r => r.LapNumber))
            {
                stint.Laps.Add(new Lap
                {
                    LapNumber = row.LapNumber,
                    LapTimeSeconds = row.LapTimeSeconds,
                    TyreLife = row.TyreLife,
                    PredictedLapTimeSeconds = row.PredictedLapTimeSeconds,
                });
            }

            session.Stints.Add(stint);
        }

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }
}
