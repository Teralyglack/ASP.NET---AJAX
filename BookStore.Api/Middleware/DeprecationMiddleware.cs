using Microsoft.Extensions.Primitives;

namespace BookStore.Api.Middleware;

/// <summary>
/// Добавляет RFC 8594 (Sunset) и Deprecation заголовки к ответам v1 API.
/// </summary>
public class DeprecationMiddleware
{
    private const string SunsetDate = "Sat, 31 Dec 2026 23:59:59 GMT";
    private const string SuccessorLink =
        "<https://localhost:5001/api/v2/books>; rel=\"successor-version\"";

    private readonly RequestDelegate _next;
    private readonly ILogger<DeprecationMiddleware> _logger;

    public DeprecationMiddleware(RequestDelegate next, ILogger<DeprecationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsV1Request(context.Request))
        {
            _logger.LogWarning(
                "Deprecated v1 endpoint called: {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["Deprecation"] = "true";
                headers["Sunset"] = SunsetDate;
                headers["Link"] = SuccessorLink;
                headers["Warning"] =
                    "299 - \"This API version is deprecated. Use /api/v2/books.\"";
                return Task.CompletedTask;
            });
        }

        await _next(context);
    }

    private static bool IsV1Request(HttpRequest request)
    {
        var path = request.Path.Value ?? string.Empty;

        // Явный v1 в пути
        if (path.Contains("/v1/", StringComparison.OrdinalIgnoreCase))
            return true;

        // Клиент запросил v1 через api-version (query или header)
        if (request.Query.TryGetValue("api-version", out var q) && q == "1.0")
            return true;
        if (request.Headers.TryGetValue("api-version", out StringValues h) && h == "1.0")
            return true;

        // Запрос без версии → по умолчанию v1
        var hasV2 = path.Contains("/v2/", StringComparison.OrdinalIgnoreCase);
        var isApiBooks = path.StartsWith("/api/books", StringComparison.OrdinalIgnoreCase)
                      || path.Equals("/api/books", StringComparison.OrdinalIgnoreCase);
        if (isApiBooks && !hasV2)
            return true;

        return false;
    }
}