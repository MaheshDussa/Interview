namespace WebApplication1.Models.DTOs;

public class TaskDto
{
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = null!;
    public bool IsCompleted { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CreatedAt { get; set; }
}
