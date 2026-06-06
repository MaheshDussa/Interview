using System;
using System.Collections.Generic;
// Note: Avoid direct Microsoft.OpenApi model types here to prevent versioning conflicts with Swashbuckle.

namespace WebApplication1.ServiceCollections;

public static class SwaggerConfiguration
{
    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        // Register Swagger generator. Keep configuration minimal to avoid
        // direct dependencies on Microsoft.OpenApi model types in this file.
        services.AddSwaggerGen();

        return services;
    }

    public static WebApplication UseSwaggerWithUI(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Management API v1");
            options.RoutePrefix = "swagger";
        });

        return app;
    }
}
