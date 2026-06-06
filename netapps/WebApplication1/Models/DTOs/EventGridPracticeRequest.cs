namespace WebApplication1.Models.DTOs;

public class EventGridPracticeRequest
{
    public string Subject { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
}