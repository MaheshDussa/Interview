using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Data;
using WebApplication1.Models.DTOs;
using WebApplication1.ServiceCollections;

namespace WebApplication1.Services;

public class AuthService : IAuthService
{
    private readonly LearningContext _context;
    private readonly IConfiguration _configuration;
    private readonly IApplicationTelemetry _telemetry;

    public AuthService(LearningContext context, IConfiguration configuration, IApplicationTelemetry telemetry)
    {
        _context = context;
        _configuration = configuration;
        _telemetry = telemetry;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive == true);

        if (user == null)
        {
            _telemetry.TrackEvent("UserLoginFailed", new Dictionary<string, string>
            {
                ["reason"] = "user_not_found_or_inactive"
            });

            return null;
        }

        var token = GenerateJwtToken(user);

        _telemetry.TrackEvent("UserLoginSucceeded", new Dictionary<string, string>
        {
            ["userId"] = user.UserId.ToString(),
            ["authType"] = "jwt"
        });

        return new LoginResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Token = token
        };
    }

    private string GenerateJwtToken(Models.Entities.User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetRequiredValue("Jwt:Key")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration.GetRequiredValue("Jwt:Issuer"),
            audience: _configuration.GetRequiredValue("Jwt:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
