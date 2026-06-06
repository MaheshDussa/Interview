namespace WebApplication1.Models.DTOs;

public class EventHubPracticeRequest
{
    public string Message { get; set; } = string.Empty;
    public string? PartitionKey { get; set; }
}