using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.DTOs;

public class CreateTaskRequest
{
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = null!;

    public DateTime? DueDate { get; set; }
}
