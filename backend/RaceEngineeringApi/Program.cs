using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceEngineeringApi.Data;
using RaceEngineeringApi.Endpoints;
using RaceEngineeringApi.Middleware;
using RaceEngineeringApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Let RACE_ENGINEERING_API_KEY override the "ApiKey" setting so ops/CI can
// inject it as a plain env var without needing the ASP.NET Core
// double-underscore convention.
var apiKeyFromEnv = Environment.GetEnvironmentVariable("RACE_ENGINEERING_API_KEY");
if (!string.IsNullOrWhiteSpace(apiKeyFromEnv))
{
    builder.Configuration["ApiKey"] = apiKeyFromEnv;
}

// DATABASE_URL (a standard postgres:// connection URL, e.g. from Neon) takes
// priority when set. Falls back to local SQLite for development, so nothing
// changes for anyone running this without a Postgres database configured.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        options.UseNpgsql(BuildNpgsqlConnectionString(databaseUrl));
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("Default")
            ?? "Data Source=race_engineering.db");
    }
});

builder.Services.AddScoped<CsvImportService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Adds an "Authorize" button in Swagger UI so the API key can be pasted
    // in once and gets sent as X-Api-Key on every "Try it out" call.
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        Description = "Cola aqui a chave da equipa (sem a palavra 'Bearer', só a chave).",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// Allow the dev Vite server AND the deployed frontend to call the API.
// FRONTEND_URL is set as an env var in production (e.g. the Vercel URL);
// localhost:5173 stays allowed so local development keeps working.
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

// Ensure the SQLite schema exists on startup (fine for a small internal tool;
// a real deployment would use EF Core migrations instead).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger stays available in production too — this is a portfolio demo
// meant to be explored by recruiters, not a real internal tool with
// sensitive data, so the usual "hide Swagger in prod" advice doesn't apply
// here. Revisit this if the tool ever holds real team data.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

// Only /api/* requires the key — /health stays open for infra health checks,
// and /swagger stays open so the docs are browsable without a key.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseApiKeyAuth());

app.MapSessionEndpoints();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Render (and most cloud hosts) assign the port dynamically via $PORT and
// expect the app to bind to 0.0.0.0, not localhost.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}

app.Run();

// Converts a standard postgres:// connection URL (the format Neon, Render,
// Railway etc. hand out) into the semicolon-delimited connection string
// Npgsql expects — the two formats are not interchangeable.
static string BuildNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var database = uri.AbsolutePath.TrimStart('/');
    var port = uri.Port == -1 ? 5432 : uri.Port;

    return new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = port,
        Database = database,
        Username = username,
        Password = password,
        SslMode = Npgsql.SslMode.Require,
        TrustServerCertificate = true,
    }.ConnectionString;
}

// Makes the implicit Program class visible to RaceEngineeringApi.Tests, so
// WebApplicationFactory<Program> can spin up this app in-memory for tests.
public partial class Program { }