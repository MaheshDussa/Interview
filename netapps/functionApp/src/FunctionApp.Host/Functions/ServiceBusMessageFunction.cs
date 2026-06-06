using FunctionApp.Host.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class ServiceBusMessageFunction(ILogger<ServiceBusMessageFunction> logger)
{
    [Function(nameof(ServiceBusMessageFunction))]
    public void Run([ServiceBusTrigger("sample-queue", Connection = ConnectionNames.ServiceBus)] string message)
    {
        logger.LogInformation("Service Bus trigger received message: {Message}", message);
    }
}