using FunctionApp.Host.Configuration;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class CosmosChangeFeedFunction(ILogger<CosmosChangeFeedFunction> logger)
{
    [Function(nameof(CosmosChangeFeedFunction))]
    public void Run(
        [CosmosDBTrigger(
            databaseName: "app-db",
            containerName: "items",
            Connection = ConnectionNames.CosmosDb,
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<string> documents)
    {
        logger.LogInformation("Cosmos DB trigger received {Count} changed document(s).", documents.Count);
    }
}