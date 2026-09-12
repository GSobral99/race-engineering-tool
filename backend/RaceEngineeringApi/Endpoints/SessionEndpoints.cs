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

        group.MapGet("/", async (AppDbContext db) =>
            await db.Sessions
                .OrderByDescending(s => s.ImportedAt)
                .Select(s => new { s.Id, s.Name, s.Source, s.ImportedAt })
                .ToListAsync());

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var session = await db.Sessions
                .Include(s => s.Stints)
                .ThenInclude(st => st.Laps)
                .FirstOrDefaultAsync(s => s.Id == id);

            return session is null ? Results.NotFound() : Results.Ok(session);
        });

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

            try
            {
                await using var stream = file.OpenReadStream();
                var session = await importer.ImportAsync(stream, sessionName, source);

                return Results.Created($"/api/sessions/{session.Id}", new { session.Id, session.Name });
            }
            catch (DbUpdateException ex)
            {
                return Results.Problem(
                    detail: ex.InnerException?.Message ?? ex.Message,
                    statusCode: 500);
            }
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