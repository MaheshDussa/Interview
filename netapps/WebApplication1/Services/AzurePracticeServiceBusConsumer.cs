using Azure.Messaging.ServiceBus;
using WebApplication1.ServiceCollections;

namespace WebApplication1.Services;

public class AzurePracticeServiceBusConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzurePracticeServiceBusConsumer> _logger;
    private readonly IApplicationTelemetry _telemetry;
    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    public AzurePracticeServiceBusConsumer(
        IConfiguration configuration,
        ILogger<AzurePracticeServiceBusConsumer> logger,
        IApplicationTelemetry telemetry)
    {
        _configuration = configuration;
        _logger = logger;
        _telemetry = telemetry;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.IsServiceBusConsumerRequested())
        {
            _logger.LogInformation("Azure practice Service Bus consumer is disabled.");
            return;
        }

        if (!_configuration.IsServiceBusPracticeConfigured())
        {
            _logger.LogWarning("Azure practice Service Bus consumer was enabled but Service Bus is not configured.");
            return;
        }

        var connectionString = _configuration.GetRequiredValue("AzurePractice:ServiceBus:ConnectionString");
        var queueName = _configuration.GetRequiredValue("AzurePractice:ServiceBus:QueueName");
        var maxConcurrentCalls = Math.Max(1, _configuration.GetValue("AzurePractice:Consumers:ServiceBus:MaxConcurrentCalls", 1));

        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = maxConcurrentCalls
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;

        _logger.LogInformation("Azure practice Service Bus consumer started for queue {QueueName}.", queueName);
        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        if (_client != null)
        {
            await _client.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        var messageId = args.Message.MessageId ?? Guid.NewGuid().ToString("N");
        var correlationId = args.Message.CorrelationId ?? messageId;
        var entityPath = args.EntityPath ?? string.Empty;

        _logger.LogInformation(
            "Processed Azure practice Service Bus message. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
            messageId,
            correlationId);

        _telemetry.TrackEvent("PracticeServiceBusReceived", new Dictionary<string, string>
        {
            ["queueName"] = entityPath,
            ["messageId"] = messageId,
            ["correlationId"] = correlationId,
            ["featureArea"] = "AzurePractice"
        }, new Dictionary<string, double>
        {
            ["messageLength"] = messageBody.Length
        });

        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Azure practice Service Bus consumer error. Entity: {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }
}