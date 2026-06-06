using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class BlobIngestFunction(ILogger<BlobIngestFunction> logger)
{
    [Function(nameof(BlobIngestFunction))]
    public void Run([BlobTrigger("samples-workitems/{name}", Connection = "AzureWebJobsStorage")] string blobContents, string name)
    {
        logger.LogInformation(
            "Blob trigger processed {BlobName} with {Length} characters.",
            name,
            blobContents.Length);
    }
}