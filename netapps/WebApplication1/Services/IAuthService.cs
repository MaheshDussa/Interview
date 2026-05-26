using WebApplication1.Models.DTOs;

namespace WebApplication1.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
