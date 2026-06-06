using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class QueueMessageFunction(ILogger<QueueMessageFunction> logger)
{
    [Function(nameof(QueueMessageFunction))]
    public void Run([QueueTrigger("sample-queue", Connection = "AzureWebJobsStorage")] string message)
    {
        logger.LogInformation("Queue trigger received message: {Message}", message);
    }
}