namespace WebApplication1.ServiceCollections;

public static class CorsConfiguration
{
    public const string AllowAngularApp = "AllowAngularApp";

    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(AllowAngularApp, policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static WebApplication UseCorsPolicy(this WebApplication app)
    {
        app.UseCors(AllowAngularApp);
        return app;
    }
}
