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
                // Allow any origin for frontend apps. Do not use AllowCredentials() together
                // with AllowAnyOrigin — browsers will reject responses. If you need to
                // support cookies or credentialed requests, replace AllowAnyOrigin with
                // a list of specific origins and keep AllowCredentials().
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
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
