namespace FunctionApp.Host.Configuration;

internal static class ConnectionNames
{
    public const string Storage = "AzureWebJobsStorage";
    public const string ServiceBus = "ServiceBusConnection";
    public const string EventHub = "EventHubConnection";
    public const string CosmosDb = "CosmosDbConnection";
}