namespace WebApplication1.Models.DTOs;

public class AzurePracticeOperationResult
{
    public string Provider { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Status { get; set; } = "Queued";
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}