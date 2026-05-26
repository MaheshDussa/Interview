using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.DTOs;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
