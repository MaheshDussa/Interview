using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using WebApplication1.ServiceCollections;

namespace WebApplication1.Middleware;

public class ApplicationInsightsRequestTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApplicationInsightsRequestTrackingMiddleware> _logger;

    public ApplicationInsightsRequestTrackingMiddleware(
        RequestDelegate next,
        ILogger<ApplicationInsightsRequestTrackingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration, IServiceProvider serviceProvider)
    {
        if (!configuration.IsApplicationInsightsConfigured())
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? requestException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            requestException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            var telemetryClient = serviceProvider.GetService<TelemetryClient>();
            if (telemetryClient != null)
            {
                var endpoint = context.GetEndpoint()?.DisplayName ?? "unmatched";
                var userId = ResolveUserId(context);
                var properties = new Dictionary<string, string>
                {
                    ["path"] = context.Request.Path.Value ?? string.Empty,
                    ["method"] = context.Request.Method,
                    ["endpoint"] = endpoint,
                    ["statusCode"] = context.Response.StatusCode.ToString(),
                    ["isAuthenticated"] = context.User.Identity?.IsAuthenticated == true ? "true" : "false",
                    ["userId"] = userId,
                    ["traceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier
                };

                if (requestException != null)
                {
                    properties["exceptionType"] = requestException.GetType().Name;
                }

                telemetryClient.TrackEvent("ApiRequestCompleted", properties, new Dictionary<string, double>
                {
                    ["durationMs"] = stopwatch.Elapsed.TotalMilliseconds
                });

                _logger.LogInformation(
                    "Request completed. Path: {Path}, StatusCode: {StatusCode}, UserId: {UserId}, DurationMs: {DurationMs}",
                    context.Request.Path,
                    context.Response.StatusCode,
                    userId,
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }
    }

    private static string ResolveUserId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return "anonymous";
        }

        return context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? context.User.FindFirst("oid")?.Value
            ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? context.User.Identity?.Name
            ?? "authenticated";
    }
}

public static class ApplicationInsightsRequestTrackingMiddlewareExtensions
{
    public static WebApplication UseApplicationInsightsRequestTracking(this WebApplication app)
    {
        app.UseMiddleware<ApplicationInsightsRequestTrackingMiddleware>();
        return app;
    }
}