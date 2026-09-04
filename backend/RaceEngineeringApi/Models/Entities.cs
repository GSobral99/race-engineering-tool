namespace RaceEngineeringApi.Models;
using System.Text.Json.Serialization;
public class Session
{
    public int Id { get; set; }
    public required string Name { get; set; }          // e.g. "2024 Silverstone - Race"
    public required string Source { get; set; }         // e.g. "ac-lap-coach", "pit-stop-predictor"
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public List<Stint> Stints { get; set; } = new();
}

public class Stint
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    
    [JsonIgnore]
    public Session? Session { get; set; }

    public required string Driver { get; set; }
    public int StintNumber { get; set; }
    public required string Compound { get; set; }       // SOFT / MEDIUM / HARD / etc.

    public List<Lap> Laps { get; set; } = new();
}

public class Lap
{
    public int Id { get; set; }
    public int StintId { get; set; }

    [JsonIgnore]
    public Stint? Stint { get; set; }

    public int LapNumber { get; set; }
    public double LapTimeSeconds { get; set; }
    public int TyreLife { get; set; }

    // Nullable: only populated when the source CSV came from a prediction model
    public double? PredictedLapTimeSeconds { get; set; }
}
