using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using MCP;
using Xunit.Abstractions;

namespace MCP.Tests;

public sealed class AzureDevOpsWorkItemClientTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task GetWorkItemAsync_ReturnsDescriptionFromAzureDevOpsResponse()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://dev.azure.com/example-org/SampleProject/_apis/wit/workitems/42?fields=System.Title,System.Description&api-version=7.1", request.RequestUri?.ToString());
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            Assert.Equal(Convert.ToBase64String(Encoding.ASCII.GetBytes(":test-pat")), request.Headers.Authorization?.Parameter);
            Assert.Contains(request.Headers.Accept, header => header.MediaType == "application/json");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    id = 42,
                    fields = new Dictionary<string, string>
                    {
                        ["System.Title"] = "Sample work item",
                        ["System.Description"] = "Fetched description from Azure DevOps"
                    }
                })
            };
        });

        using var httpClient = new HttpClient(handler);
        var options = Options.Create(new AzureDevOpsOptions
        {
            OrganizationUrl = "https://dev.azure.com/example-org",
            Project = "SampleProject",
            Pat = "test-pat",
            ApiVersion = "7.1"
        });
        var client = new AzureDevOpsWorkItemClient(httpClient, options);

        var workItem = await client.GetWorkItemAsync(42);

        _output.WriteLine(workItem.Description ?? string.Empty);

        Assert.Equal(42, workItem.Id);
        Assert.Equal("Sample work item", workItem.Title);
        Assert.Equal("Fetched description from Azure DevOps", workItem.Description);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}