using FunctionApp.Host.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class EventHubMessageFunction(ILogger<EventHubMessageFunction> logger)
{
    [Function(nameof(EventHubMessageFunction))]
    public void Run([EventHubTrigger("%SampleEventHubName%", Connection = ConnectionNames.EventHub)] string[] messages)
    {
        logger.LogInformation("Event Hubs trigger received {Count} message(s).", messages.Length);
    }
}