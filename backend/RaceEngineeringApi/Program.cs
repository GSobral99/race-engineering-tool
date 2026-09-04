using Microsoft.EntityFrameworkCore;
using RaceEngineeringApi.Data;
using RaceEngineeringApi.Endpoints;
using RaceEngineeringApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=race_engineering.db"));

builder.Services.AddScoped<CsvImportService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow the local Vite dev server to call the API during development.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Ensure the SQLite schema exists on startup (fine for a small internal tool;
// a real deployment would use EF Core migrations instead).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.MapSessionEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
