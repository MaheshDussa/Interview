namespace WebApplication1.Models.DTOs;

public class ServiceBusPracticeRequest
{
    public string Message { get; set; } = string.Empty;
    public string? MessageId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Subject { get; set; }
}