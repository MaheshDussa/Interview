using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class EventGridNotificationFunction(ILogger<EventGridNotificationFunction> logger)
{
    [Function(nameof(EventGridNotificationFunction))]
    public void Run([EventGridTrigger] string eventGridEvent)
    {
        logger.LogInformation("Event Grid trigger received payload: {Payload}", eventGridEvent);
    }
}