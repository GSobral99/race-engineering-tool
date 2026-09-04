using Microsoft.EntityFrameworkCore;
using RaceEngineeringApi.Data;
using RaceEngineeringApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace RaceEngineeringApi.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sessions").WithTags("Sessions");

        // List all imported sessions, newest first.
        group.MapGet("/", async (AppDbContext db) =>
            await db.Sessions
                .OrderByDescending(s => s.ImportedAt)
                .Select(s => new { s.Id, s.Name, s.Source, s.ImportedAt })
                .ToListAsync());

        // Full detail for one session: all stints and their laps.
        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var session = await db.Sessions
                .Include(s => s.Stints)
                .ThenInclude(st => st.Laps)
                .FirstOrDefaultAsync(s => s.Id == id);

            return session is null ? Results.NotFound() : Results.Ok(session);
        });

        // Just the stints for a session, useful for a lighter "overview" view.
        group.MapGet("/{id:int}/stints", async (int id, AppDbContext db) =>
            await db.Stints
                .Where(st => st.SessionId == id)
                .Select(st => new
                {
                    st.Id,
                    st.Driver,
                    st.StintNumber,
                    st.Compound,
                    LapCount = st.Laps.Count,
                    AvgLapTime = st.Laps.Average(l => l.LapTimeSeconds),
                })
                .ToListAsync());

        // Upload a CSV (from ac-lap-coach or the pit-stop-predictor) and store it.
       group.MapPost("/import", async (
            IFormFile file,
            [FromForm] string? sessionName,
            [FromForm] string? source,
            CsvImportService importer) =>
        {
            if (string.IsNullOrWhiteSpace(sessionName))
                sessionName = file.FileName;
            if (string.IsNullOrWhiteSpace(source))
                source = "unknown";

            await using var stream = file.OpenReadStream();
            var session = await importer.ImportAsync(stream, sessionName, source);

            return Results.Created($"/api/sessions/{session.Id}", new { session.Id, session.Name });
        })
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data");

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var session = await db.Sessions.FindAsync(id);
            if (session is null) return Results.NotFound();

            db.Sessions.Remove(session);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
