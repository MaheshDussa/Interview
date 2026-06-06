using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Core;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Queues;
using WebApplication1.Models.DTOs;
using WebApplication1.ServiceCollections;

namespace WebApplication1.Services;

public class AzurePracticeMessagingService : IAzurePracticeMessagingService
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationTelemetry _telemetry;

    public AzurePracticeMessagingService(IConfiguration configuration, IApplicationTelemetry telemetry)
    {
        _configuration = configuration;
        _telemetry = telemetry;
    }

    public async Task<AzurePracticeOperationResult> PublishEventGridAsync(EventGridPracticeRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePracticeEnabled(_configuration.IsEventGridPracticeConfigured(), "Event Grid");
        ValidateRequired(request.Subject, nameof(request.Subject));
        ValidateRequired(request.EventType, nameof(request.EventType));

        var endpoint = new Uri(_configuration.GetRequiredValue("AzurePractice:EventGrid:Endpoint"));
        var accessKey = _configuration.GetRequiredValue("AzurePractice:EventGrid:AccessKey");
        var client = new EventGridPublisherClient(endpoint, new AzureKeyCredential(accessKey));
        var correlationId = Guid.NewGuid().ToString("N");

        var eventGridEvent = new EventGridEvent(
            request.Subject,
            request.EventType,
            "1.0",
            BinaryData.FromString(string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson));

        await client.SendEventAsync(eventGridEvent, cancellationToken);

        _telemetry.TrackEvent("PracticeEventGridPublished", BuildProperties("EventGrid", correlationId));

        return CreateResult("EventGrid", endpoint.Host, correlationId);
    }

    public async Task<AzurePracticeOperationResult> SendEventHubAsync(EventHubPracticeRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePracticeEnabled(_configuration.IsEventHubPracticeConfigured(), "Event Hubs");
        ValidateRequired(request.Message, nameof(request.Message));

        var connectionString = _configuration.GetRequiredValue("AzurePractice:EventHubs:ConnectionString");
        var hubName = _configuration.GetRequiredValue("AzurePractice:EventHubs:HubName");
        var correlationId = Guid.NewGuid().ToString("N");

        await using var producerClient = new EventHubProducerClient(connectionString, hubName);
        var options = string.IsNullOrWhiteSpace(request.PartitionKey)
            ? new CreateBatchOptions()
            : new CreateBatchOptions { PartitionKey = request.PartitionKey };
        using var batch = await producerClient.CreateBatchAsync(options, cancellationToken);

        var eventData = new EventData(request.Message);
        eventData.Properties["correlationId"] = correlationId;

        if (!batch.TryAdd(eventData))
        {
            throw new ValidationException("The Event Hub message is too large for a single batch.");
        }

        await producerClient.SendAsync(batch, cancellationToken);

        _telemetry.TrackEvent("PracticeEventHubSent", BuildProperties("EventHubs", correlationId));

        return CreateResult("EventHubs", hubName, correlationId);
    }

    public async Task<AzurePracticeOperationResult> SendServiceBusAsync(ServiceBusPracticeRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePracticeEnabled(_configuration.IsServiceBusPracticeConfigured(), "Service Bus");
        ValidateRequired(request.Message, nameof(request.Message));

        var connectionString = _configuration.GetRequiredValue("AzurePractice:ServiceBus:ConnectionString");
        var queueName = _configuration.GetRequiredValue("AzurePractice:ServiceBus:QueueName");
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? Guid.NewGuid().ToString("N") : request.CorrelationId;

        await using var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender(queueName);
        var message = new ServiceBusMessage(request.Message)
        {
            MessageId = string.IsNullOrWhiteSpace(request.MessageId) ? Guid.NewGuid().ToString("N") : request.MessageId,
            CorrelationId = correlationId,
            Subject = request.Subject
        };

        await sender.SendMessageAsync(message, cancellationToken);

        _telemetry.TrackEvent("PracticeServiceBusSent", BuildProperties("ServiceBus", correlationId));

        return CreateResult("ServiceBus", queueName, correlationId);
    }

    public async Task<AzurePracticeOperationResult> EnqueueStorageQueueAsync(QueueStoragePracticeRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePracticeEnabled(_configuration.IsQueueStoragePracticeConfigured(), "Queue Storage");
        ValidateRequired(request.Message, nameof(request.Message));

        var connectionString = _configuration.GetRequiredValue("AzurePractice:QueueStorage:ConnectionString");
        var queueName = _configuration.GetRequiredValue("AzurePractice:QueueStorage:QueueName");
        var correlationId = Guid.NewGuid().ToString("N");

        var queueClient = new QueueClient(connectionString, queueName.ToLowerInvariant());
        await queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await queueClient.SendMessageAsync(request.Message, cancellationToken);

        _telemetry.TrackEvent("PracticeQueueStorageSent", BuildProperties("QueueStorage", correlationId));

        return CreateResult("QueueStorage", queueName, correlationId);
    }

    private void ValidatePracticeEnabled(bool providerConfigured, string providerName)
    {
        if (!_configuration.IsAzurePracticeEnabled())
        {
            throw new InvalidOperationException("Azure practice features are disabled for this environment.");
        }

        if (!providerConfigured)
        {
            throw new InvalidOperationException($"{providerName} practice is not configured for this environment.");
        }
    }

    private static void ValidateRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{fieldName} is required.");
        }
    }

    private static AzurePracticeOperationResult CreateResult(string provider, string target, string correlationId)
    {
        return new AzurePracticeOperationResult
        {
            Provider = provider,
            Target = target,
            CorrelationId = correlationId
        };
    }

    private static Dictionary<string, string> BuildProperties(string provider, string correlationId)
    {
        return new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["correlationId"] = correlationId,
            ["featureArea"] = "AzurePractice"
        };
    }
}