using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.DTOs;

public class UpdateTaskRequest
{
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = null!;

    public bool IsCompleted { get; set; }

    public DateTime? DueDate { get; set; }
}
