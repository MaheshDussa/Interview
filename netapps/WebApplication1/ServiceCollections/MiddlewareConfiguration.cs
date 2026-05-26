namespace WebApplication1.ServiceCollections;

public static class MiddlewareConfiguration
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
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
