using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace Skyfall.Api.Middleware;

public sealed class CorsMiddleware : IFunctionsWorkerMiddleware
{
    private readonly IConfiguration _configuration;

    public CorsMiddleware(IConfiguration configuration) => _configuration = configuration;

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var request = await context.GetHttpRequestDataAsync();
        if (request is null) { await next(context); return; }

        var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var origin = GetHeader(request.Headers, "Origin");
        var allowAll = allowedOrigins.Any(o => o == "*");
        var allowOrigin = ResolveAllowedOrigin(origin, allowedOrigins, allowAll);

        if (string.IsNullOrWhiteSpace(allowOrigin) && IsLocalhost(origin))
            allowOrigin = origin;

        var requestHeaders = GetHeader(request.Headers, "Access-Control-Request-Headers");

        if (string.Equals(request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            var preflight = request.CreateResponse(HttpStatusCode.NoContent);
            AddCorsHeaders(preflight, allowOrigin, requestHeaders);
            context.GetInvocationResult().Value = preflight;
            return;
        }

        await next(context);

        if (context.GetInvocationResult().Value is HttpResponseData response)
            AddCorsHeaders(response, allowOrigin, requestHeaders);
    }

    private static void AddCorsHeaders(HttpResponseData response, string? allowOrigin, string? requestHeaders)
    {
        if (!string.IsNullOrWhiteSpace(allowOrigin))
        {
            SetHeader(response, "Access-Control-Allow-Origin", allowOrigin);
            if (allowOrigin != "*")
                SetHeader(response, "Access-Control-Allow-Credentials", "true");
            else
                response.Headers.Remove("Access-Control-Allow-Credentials");
        }

        SetHeader(response, "Vary", "Origin");
        SetHeader(response, "Access-Control-Allow-Methods", "GET,POST,PUT,PATCH,DELETE,OPTIONS");
        SetHeader(response, "Access-Control-Allow-Headers",
            string.IsNullOrWhiteSpace(requestHeaders) ? "Authorization,Content-Type" : requestHeaders);
    }

    private static bool IsLocalhost(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;
        return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetHeader(HttpHeadersCollection headers, string name) =>
        headers.FirstOrDefault(h => string.Equals(h.Key, name, StringComparison.OrdinalIgnoreCase)).Value?.FirstOrDefault();

    private static string? ResolveAllowedOrigin(string? requestOrigin, string[] allowedOrigins, bool allowAll)
    {
        if (allowAll) return "*";
        if (string.IsNullOrWhiteSpace(requestOrigin)) return null;
        var normalized = requestOrigin.Trim().TrimEnd('/');
        return allowedOrigins.FirstOrDefault(a =>
            string.Equals(a.Trim().TrimEnd('/'), normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static void SetHeader(HttpResponseData response, string name, string value)
    {
        response.Headers.Remove(name);
        response.Headers.Add(name, value);
    }
}
