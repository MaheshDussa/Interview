using System.Security.Claims;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace WebApplication1.Middleware;

public class UserTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        telemetry.Context.Cloud.RoleName = environment.ApplicationName;
        telemetry.Context.GlobalProperties["application"] = environment.ApplicationName;
        telemetry.Context.GlobalProperties["environment"] = environment.EnvironmentName;

        if (telemetry is DependencyTelemetry dependencyTelemetry)
        {
            dependencyTelemetry.Properties["environment"] = environment.EnvironmentName;
            dependencyTelemetry.Properties["application"] = environment.ApplicationName;
            dependencyTelemetry.Properties["requestPath"] = httpContext.Request.Path.Value ?? string.Empty;
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = ResolveUserId(httpContext.User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        telemetry.Context.User.AuthenticatedUserId = userId;
        telemetry.Context.User.Id = userId;
    }

    private static string? ResolveUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("oid")?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.Identity?.Name;
    }
}