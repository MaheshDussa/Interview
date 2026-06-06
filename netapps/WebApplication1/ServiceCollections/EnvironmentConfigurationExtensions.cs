namespace WebApplication1.ServiceCollections;

public static class EnvironmentConfigurationExtensions
{
    public static WebApplicationBuilder AddNormalizedEnvironmentSettings(this WebApplicationBuilder builder)
    {
        var normalizedEnvironment = NormalizeEnvironmentName(builder.Environment.EnvironmentName);
        var normalizedFileName = $"appsettings.{normalizedEnvironment}.json";

        builder.Configuration.AddJsonFile(normalizedFileName, optional: true, reloadOnChange: true);

        return builder;
    }

    private static string NormalizeEnvironmentName(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return "Production";
        }

        return environmentName.Trim().ToLowerInvariant() switch
        {
            "dev" => "Development",
            "development" => "Development",
            "qa" => "QA",
            "stage" => "Stage",
            "staging" => "Stage",
            "prod" => "Prod",
            "production" => "Prod",
            _ => environmentName.Trim()
        };
    }
}