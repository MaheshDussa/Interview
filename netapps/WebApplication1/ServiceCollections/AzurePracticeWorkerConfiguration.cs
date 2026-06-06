using WebApplication1.Services;

namespace WebApplication1.ServiceCollections;

public static class AzurePracticeWorkerConfiguration
{
    public static IServiceCollection AddAzurePracticeBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<AzurePracticeServiceBusConsumer>();
        services.AddHostedService<AzurePracticeQueueStorageConsumer>();

        return services;
    }
}