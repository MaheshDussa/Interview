using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace WebApplication1.ServiceCollections;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration.GetRequiredValue("Jwt:Issuer"),
                ValidAudience = configuration.GetRequiredValue("Jwt:Audience"),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetRequiredValue("Jwt:Key")))
            };
        });

        return services;
    }

    public static IServiceCollection AddAzureAdAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure JWT Bearer to validate tokens issued by Azure AD v2.0
        var azureAd = configuration.GetSection("AzureAd");
        var instance = azureAd.GetValue<string>("Instance") ?? "https://login.microsoftonline.com/";
        var tenantId = azureAd.GetValue<string>("TenantId") ?? "common";
        var clientId = configuration.GetRequiredValue("AzureAd:ClientId");

        var authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            // Accept tokens for this API if the 'aud' claim contains the clientId or the api://{clientId}
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidAudiences = new[] { clientId, $"api://{clientId}" }
            };
        });

        return services;
    }
}
