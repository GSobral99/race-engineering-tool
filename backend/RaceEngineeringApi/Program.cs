using Microsoft.EntityFrameworkCore;
using RaceEngineeringApi.Data;
using RaceEngineeringApi.Endpoints;
using RaceEngineeringApi.Middleware;
using RaceEngineeringApi.Services;

var builder = WebApplication.CreateBuilder(args);

var apiKeyFromEnv = Environment.GetEnvironmentVariable("RACE_ENGINEERING_API_KEY");
if (!string.IsNullOrWhiteSpace(apiKeyFromEnv))
{
    builder.Configuration["ApiKey"] = apiKeyFromEnv;
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=race_engineering.db"));

builder.Services.AddScoped<CsvImportService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = new List<string> { "http://localhost:5173" };
        if (!string.IsNullOrWhiteSpace(frontendUrl))
            origins.Add(frontendUrl.TrimEnd('/'));

        policy.WithOrigins(origins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();


app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseApiKeyAuth());

app.MapSessionEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();