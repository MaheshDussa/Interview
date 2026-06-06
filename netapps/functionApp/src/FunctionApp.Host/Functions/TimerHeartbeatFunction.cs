using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class TimerHeartbeatFunction(ILogger<TimerHeartbeatFunction> logger)
{
    [Function(nameof(TimerHeartbeatFunction))]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
    {
        logger.LogInformation(
            "Timer trigger fired at {Timestamp}. Next occurrence: {NextOccurrence}.",
            DateTimeOffset.UtcNow,
            timerInfo.ScheduleStatus?.Next);
    }
}