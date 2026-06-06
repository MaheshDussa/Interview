using Azure.Identity;

namespace WebApplication1.ServiceCollections;

public static class SecretConfigurationExtensions
{
    private const string KeyVaultUriKey = "KeyVault:VaultUri";
    private static readonly TimeSpan KeyVaultReloadInterval = TimeSpan.FromMinutes(15);

    public static WebApplicationBuilder AddCentralizedSecrets(this WebApplicationBuilder builder)
    {
        var vaultUri = builder.Configuration[KeyVaultUriKey];
        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            return builder;
        }

        if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out var keyVaultUri))
        {
            throw new InvalidOperationException($"Configuration value '{KeyVaultUriKey}' must be a valid absolute URI.");
        }

        builder.Configuration.AddAzureKeyVault(
            keyVaultUri,
            new DefaultAzureCredential(),
            new Azure.Extensions.AspNetCore.Configuration.Secrets.AzureKeyVaultConfigurationOptions
            {
                ReloadInterval = KeyVaultReloadInterval
            });

        return builder;
    }

    public static void ValidateRequiredSecrets(this IConfiguration configuration)
    {
        var missingKeys = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("MyExpressConnection")))
        {
            missingKeys.Add("ConnectionStrings:MyExpressConnection");
        }

        var azureClientId = configuration["AzureAd:ClientId"];
        if (string.IsNullOrWhiteSpace(azureClientId))
        {
            AddIfMissing(configuration, missingKeys, "Jwt:Key");
            AddIfMissing(configuration, missingKeys, "Jwt:Issuer");
            AddIfMissing(configuration, missingKeys, "Jwt:Audience");
        }

        if (missingKeys.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Missing required configuration values. Provide them through Azure Key Vault, user-secrets, environment variables, or appsettings: " +
            string.Join(", ", missingKeys));
    }

    public static string GetRequiredConnectionString(this IConfiguration configuration, string name)
    {
        var value = configuration.GetConnectionString(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Connection string 'ConnectionStrings:{name}' is required.");
    }

    public static string GetRequiredValue(this IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException($"Configuration value '{key}' is required.");
    }

    public static bool IsBlobStorageConfigured(this IConfiguration configuration)
    {
        return HasValue(configuration, "BlobStorage:ConnectionString") &&
               HasValue(configuration, "BlobStorage:ContainerName");
    }

    public static bool IsApplicationInsightsConfigured(this IConfiguration configuration)
    {
        return HasValue(configuration, "ApplicationInsights:ConnectionString");
    }

    public static bool IsAzurePracticeEnabled(this IConfiguration configuration)
    {
        return configuration.GetValue("AzurePractice:Enabled", false);
    }

    public static bool IsEventGridPracticeConfigured(this IConfiguration configuration)
    {
        return HasValue(configuration, "AzurePractice:EventGrid:Endpoint") &&
               HasValue(configuration, "AzurePractice:EventGrid:AccessKey");
    }

    public static bool IsEventHubPracticeConfigured(this IConfiguration configuration)
    {
        return HasValue(configuration, "AzurePractice:EventHubs:ConnectionString") &&
               HasValue(configuration, "AzurePractice:EventHubs:HubName");
    }

    public static bool IsServiceBusPracticeConfigured(this IConfiguration configuration)
    {
        return HasValue(configuration, "AzurePractice:ServiceBus:ConnectionString") &&
               HasValue(configuration, "AzurePractice:ServiceBus:QueueName");
    }

    public static bool IsQueueStoragePracticeConfigured(this IConfiguration configuration)
    {
        return HasValue(configuration, "AzurePractice:QueueStorage:ConnectionString") &&
               HasValue(configuration, "AzurePractice:QueueStorage:QueueName");
    }

    public static bool IsServiceBusConsumerRequested(this IConfiguration configuration)
    {
        return configuration.IsAzurePracticeEnabled() &&
               configuration.GetValue("AzurePractice:Consumers:ServiceBus:Enabled", false);
    }

    public static bool IsQueueStorageConsumerRequested(this IConfiguration configuration)
    {
        return configuration.IsAzurePracticeEnabled() &&
               configuration.GetValue("AzurePractice:Consumers:QueueStorage:Enabled", false);
    }

    private static bool HasValue(IConfiguration configuration, string key)
    {
        return !string.IsNullOrWhiteSpace(configuration[key]);
    }

    private static void AddIfMissing(IConfiguration configuration, ICollection<string> missingKeys, string key)
    {
        if (!HasValue(configuration, key))
        {
            missingKeys.Add(key);
        }
    }
}