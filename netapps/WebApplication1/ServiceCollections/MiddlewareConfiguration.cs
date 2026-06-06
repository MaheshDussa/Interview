namespace WebApplication1.ServiceCollections;

using WebApplication1.Middleware;

public static class MiddlewareConfiguration
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Global exception handling middleware should be first in the pipeline so it
        // can catch exceptions from all downstream middleware and controllers.
        app.UseExceptionHandlingMiddleware();
        app.UseApplicationInsightsRequestTracking();

        app.UseSwaggerWithUI();

        app.UseHttpsRedirection();

        // CORS must be called before Authentication/Authorization
        app.UseCorsPolicy();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}
