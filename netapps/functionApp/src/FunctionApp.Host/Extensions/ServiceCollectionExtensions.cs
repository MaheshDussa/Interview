using Microsoft.Extensions.DependencyInjection;

namespace FunctionApp.Host.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFunctionAppObservability(this IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        return services;
    }
}