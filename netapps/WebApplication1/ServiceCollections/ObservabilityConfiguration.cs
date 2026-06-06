using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using WebApplication1.Middleware;
using WebApplication1.Services;

namespace WebApplication1.ServiceCollections;

public static class ObservabilityConfiguration
{
    public static IServiceCollection AddApplicationObservability(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IApplicationTelemetry, NoOpApplicationTelemetry>();

        if (!configuration.IsApplicationInsightsConfigured())
        {
            return services;
        }

        services.AddApplicationInsightsTelemetry(new ApplicationInsightsServiceOptions
        {
            ConnectionString = configuration.GetRequiredValue("ApplicationInsights:ConnectionString"),
            EnableAdaptiveSampling = configuration.GetValue("ApplicationInsights:EnableAdaptiveSampling", false)
        });

        services.AddSingleton<ITelemetryInitializer, UserTelemetryInitializer>();
        services.AddSingleton<IApplicationTelemetry, ApplicationInsightsTelemetry>();

        return services;
    }
}