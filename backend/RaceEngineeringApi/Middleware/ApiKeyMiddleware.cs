using System.Security.Cryptography;
using System.Text;

namespace RaceEngineeringApi.Middleware;

public class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;
    private readonly string? _configuredKey;
    private readonly ILogger<ApiKeyMiddleware> _logger;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _configuredKey = configuration["ApiKey"];

        if (string.IsNullOrWhiteSpace(_configuredKey))
        {
            _logger.LogWarning(
                "No ApiKey configured - /api/* endpoints are UNPROTECTED. " +
                "Set the 'ApiKey' setting or RACE_ENGINEERING_API_KEY environment variable before running this for a team.");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Auth disabled (no key configured): let everything through.
        if (string.IsNullOrWhiteSpace(_configuredKey))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) || !IsValidKey(provided.ToString()))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Missing or invalid API key.",
                hint = $"Send the team API key in the '{HeaderName}' header."
            });
            return;
        }

        await _next(context);
    }

    private bool IsValidKey(string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(_configuredKey!);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        if (expectedBytes.Length != providedBytes.Length)
        {
            CryptographicOperations.FixedTimeEquals(expectedBytes, expectedBytes);
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder app)
        => app.UseMiddleware<ApiKeyMiddleware>();
}