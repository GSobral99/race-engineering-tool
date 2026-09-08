using Microsoft.EntityFrameworkCore;
using RaceEngineeringApi.Data;
using RaceEngineeringApi.Endpoints;
using RaceEngineeringApi.Middleware;
using RaceEngineeringApi.Services;
using Microsoft.OpenApi.Models;

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
builder.Services.AddSwaggerGen(options =>
{
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