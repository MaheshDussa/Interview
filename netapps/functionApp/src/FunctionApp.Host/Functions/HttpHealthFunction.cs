using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FunctionApp.Host.Functions;

public sealed class HttpHealthFunction(ILogger<HttpHealthFunction> logger)
{
    [Function(nameof(HttpHealthFunction))]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequestData request)
    {
        logger.LogInformation("Processing HTTP health request.");

        var response = request.CreateResponse(HttpStatusCode.OK);
        response.WriteString($"Function app is healthy at {DateTimeOffset.UtcNow:O}.");

        return response;
    }
}