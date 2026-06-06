using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs;
using WebApplication1.ServiceCollections;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/practice/azure")]
[Authorize]
public class AzurePracticeController : ControllerBase
{
    private readonly IAzurePracticeMessagingService _messagingService;
    private readonly IConfiguration _configuration;

    public AzurePracticeController(IAzurePracticeMessagingService messagingService, IConfiguration configuration)
    {
        _messagingService = messagingService;
        _configuration = configuration;
    }

    [HttpPost("event-grid")]
    public async Task<IActionResult> PublishEventGrid([FromBody] EventGridPracticeRequest request, CancellationToken cancellationToken)
    {
        if (!_configuration.IsEventGridPracticeConfigured())
        {
            return ServiceUnavailable("Event Grid practice is disabled or not configured.");
        }

        var result = await _messagingService.PublishEventGridAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("event-hub")]
    public async Task<IActionResult> PublishEventHub([FromBody] EventHubPracticeRequest request, CancellationToken cancellationToken)
    {
        if (!_configuration.IsEventHubPracticeConfigured())
        {
            return ServiceUnavailable("Event Hubs practice is disabled or not configured.");
        }

        var result = await _messagingService.SendEventHubAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("service-bus")]
    public async Task<IActionResult> SendServiceBus([FromBody] ServiceBusPracticeRequest request, CancellationToken cancellationToken)
    {
        if (!_configuration.IsServiceBusPracticeConfigured())
        {
            return ServiceUnavailable("Service Bus practice is disabled or not configured.");
        }

        var result = await _messagingService.SendServiceBusAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("queue-storage")]
    public async Task<IActionResult> EnqueueQueueStorage([FromBody] QueueStoragePracticeRequest request, CancellationToken cancellationToken)
    {
        if (!_configuration.IsQueueStoragePracticeConfigured())
        {
            return ServiceUnavailable("Queue Storage practice is disabled or not configured.");
        }

        var result = await _messagingService.EnqueueStorageQueueAsync(request, cancellationToken);
        return Ok(result);
    }

    private ObjectResult ServiceUnavailable(string message)
    {
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message });
    }
}