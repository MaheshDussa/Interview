using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using MCP;
using Xunit.Abstractions;

namespace MCP.Tests;

public sealed class AzureOpenAiEmbeddingClientTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task GenerateEmbeddingAsync_ReturnsVectorForSamplePhrase()
    {
        var handler = new StubHttpMessageHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://example-openai.openai.azure.com/openai/deployments/text-embedding-3-small/embeddings?api-version=2024-02-01", request.RequestUri?.ToString());
            Assert.Equal("test-api-key", request.Headers.GetValues("api-key").Single());

            var requestBody = await request.Content!.ReadAsStringAsync();
            Assert.Contains("design an architecture search workflow", requestBody, StringComparison.Ordinal);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    data = new[]
                    {
                        new
                        {
                            embedding = new[] { 0.12f, -0.34f, 0.56f, 0.78f }
                        }
                    }
                })
            };
        });

        using var httpClient = new HttpClient(handler);
        var options = Options.Create(new AzureOpenAiEmbeddingsOptions
        {
            Endpoint = "https://example-openai.openai.azure.com",
            ApiKey = "test-api-key",
            EmbeddingDeployment = "text-embedding-3-small",
            ApiVersion = "2024-02-01"
        });
        var client = new AzureOpenAiEmbeddingClient(httpClient, options);

        var embedding = await client.GenerateEmbeddingAsync("design an architecture search workflow");

        _output.WriteLine(string.Join(", ", embedding.Vector.Select(value => value.ToString("0.00"))));

        Assert.Equal(4, embedding.Vector.Count);
        Assert.Equal(0.12f, embedding.Vector[0]);
        Assert.Equal(-0.34f, embedding.Vector[1]);
        Assert.Equal(0.56f, embedding.Vector[2]);
        Assert.Equal(0.78f, embedding.Vector[3]);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handler(request);
        }
    }
}