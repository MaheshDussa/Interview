using Azure.Storage.Queues;
using WebApplication1.ServiceCollections;

namespace WebApplication1.Services;

public class AzurePracticeQueueStorageConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzurePracticeQueueStorageConsumer> _logger;
    private readonly IApplicationTelemetry _telemetry;

    public AzurePracticeQueueStorageConsumer(
        IConfiguration configuration,
        ILogger<AzurePracticeQueueStorageConsumer> logger,
        IApplicationTelemetry telemetry)
    {
        _configuration = configuration;
        _logger = logger;
        _telemetry = telemetry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.IsQueueStorageConsumerRequested())
        {
            _logger.LogInformation("Azure practice Queue Storage consumer is disabled.");
            return;
        }

        if (!_configuration.IsQueueStoragePracticeConfigured())
        {
            _logger.LogWarning("Azure practice Queue Storage consumer was enabled but Queue Storage is not configured.");
            return;
        }

        var connectionString = _configuration.GetRequiredValue("AzurePractice:QueueStorage:ConnectionString");
        var queueName = _configuration.GetRequiredValue("AzurePractice:QueueStorage:QueueName").ToLowerInvariant();
        var pollingInterval = TimeSpan.FromSeconds(Math.Max(1, _configuration.GetValue("AzurePractice:Consumers:QueueStorage:PollingIntervalSeconds", 15)));
        var visibilityTimeout = TimeSpan.FromSeconds(Math.Max(5, _configuration.GetValue("AzurePractice:Consumers:QueueStorage:VisibilityTimeoutSeconds", 30)));
        var maxMessages = Math.Clamp(_configuration.GetValue("AzurePractice:Consumers:QueueStorage:MaxMessagesPerPoll", 5), 1, 32);

        var queueClient = new QueueClient(connectionString, queueName);
        await queueClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
        _logger.LogInformation("Azure practice Queue Storage consumer started for queue {QueueName}.", queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await queueClient.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken: stoppingToken);

                if (response.Value.Length == 0)
                {
                    await Task.Delay(pollingInterval, stoppingToken);
                    continue;
                }

                foreach (var message in response.Value)
                {
                    var body = message.MessageText ?? string.Empty;

                    _logger.LogInformation(
                        "Processed Azure practice Queue Storage message. MessageId: {MessageId}",
                        message.MessageId);

                    _telemetry.TrackEvent("PracticeQueueStorageReceived", new Dictionary<string, string>
                    {
                        ["queueName"] = queueName,
                        ["messageId"] = message.MessageId,
                        ["featureArea"] = "AzurePractice"
                    }, new Dictionary<string, double>
                    {
                        ["messageLength"] = body.Length
                    });

                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure practice Queue Storage consumer error.");
                await Task.Delay(pollingInterval, stoppingToken);
            }
        }
    }
}