using WebApplication1.Models.DTOs;

namespace WebApplication1.Services;

public interface IAzurePracticeMessagingService
{
    Task<AzurePracticeOperationResult> PublishEventGridAsync(EventGridPracticeRequest request, CancellationToken cancellationToken = default);
    Task<AzurePracticeOperationResult> SendEventHubAsync(EventHubPracticeRequest request, CancellationToken cancellationToken = default);
    Task<AzurePracticeOperationResult> SendServiceBusAsync(ServiceBusPracticeRequest request, CancellationToken cancellationToken = default);
    Task<AzurePracticeOperationResult> EnqueueStorageQueueAsync(QueueStoragePracticeRequest request, CancellationToken cancellationToken = default);
}