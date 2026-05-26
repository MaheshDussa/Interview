namespace WebApplication1.Models.DTOs;

public class LoginResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Token { get; set; } = null!;
}
