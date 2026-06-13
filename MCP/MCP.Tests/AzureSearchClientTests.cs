using Microsoft.Extensions.Options;
using MCP;
using Xunit.Abstractions;

namespace MCP.Tests;

public sealed class AzureSearchClientTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public async Task SearchTitlesAsync_ReturnsMatchingDocumentTitles()
    {
        var queryExecutor = new StubAzureSearchQueryExecutor(
        [
            new Dictionary<string, object?> { ["title"] = "System Architecture Overview" },
            new Dictionary<string, object?> { ["title"] = "Deployment Architecture Guide" },
            new Dictionary<string, object?> { ["summary"] = "Ignored without a title" }
        ]);
        var options = Options.Create(new AzureSearchOptions
        {
            TitleField = "title",
            MaxResults = 5
        });
        var client = new ArchitectureDocumentSearchClient(queryExecutor, options);

        var titles = await client.SearchTitlesAsync("architecture");

        foreach (var title in titles)
        {
            _output.WriteLine(title.Title);
        }

        Assert.Equal(2, titles.Count);
        Assert.Equal("System Architecture Overview", titles[0].Title);
        Assert.Equal("Deployment Architecture Guide", titles[1].Title);
    }

    [Fact]
    public async Task SearchParagraphsAsync_ReturnsParagraphLevelMatchesForArchitectureQuery()
    {
        var queryExecutor = new StubAzureSearchQueryExecutor(
            titleDocuments:
            [
                new Dictionary<string, object?> { ["title"] = "Unused in hybrid test" }
            ],
            hybridDocuments:
            [
                new Dictionary<string, object?>
                {
                    ["title"] = "Gateway Security Design",
                    ["content"] = "The API gateway routes external traffic to internal services.\n\nManaged identity secures service-to-service calls between the gateway, worker API, and search indexing pipeline. The indexing job writes chunks to Azure AI Search after token validation."
                },
                new Dictionary<string, object?>
                {
                    ["title"] = "Search Processing Overview",
                    ["content"] = "Documents are split into paragraphs before indexing.\n\nThe enrichment worker generates embeddings and stores vectors beside searchable content for hybrid retrieval."
                }
            ]);
        var embeddingClient = new StubEmbeddingClient([0.4f, 0.8f, -0.2f]);
        var options = Options.Create(new AzureSearchOptions
        {
            TitleField = "title",
            ContentField = "content",
            VectorField = "contentVector",
            MaxResults = 5
        });
        var client = new HybridArchitectureDocumentSearchClient(queryExecutor, embeddingClient, options);

        var matches = await client.SearchParagraphsAsync("how does the api gateway secure service-to-service calls to the search indexing pipeline");

        foreach (var match in matches)
        {
            _output.WriteLine($"{match.Title}: {match.Paragraph}");
        }

        Assert.Equal(2, matches.Count);
        Assert.Equal("Gateway Security Design", matches[0].Title);
        Assert.Contains("Managed identity secures service-to-service calls", matches[0].Paragraph);
        Assert.Equal("Search Processing Overview", matches[1].Title);
        Assert.Contains("stores vectors beside searchable content for hybrid retrieval", matches[1].Paragraph);
    }

    private sealed class StubAzureSearchQueryExecutor(
        IReadOnlyList<IReadOnlyDictionary<string, object?>> titleDocuments,
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? hybridDocuments = null) : IAzureSearchQueryExecutor
    {
        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SearchAsync(string keyword, int size, CancellationToken cancellationToken = default)
        {
            Assert.Equal("architecture", keyword);
            Assert.Equal(5, size);

            return Task.FromResult(titleDocuments);
        }

        public Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SearchHybridAsync(string keyword, IReadOnlyList<float> vector, int size, CancellationToken cancellationToken = default)
        {
            Assert.Equal("how does the api gateway secure service-to-service calls to the search indexing pipeline", keyword);
            Assert.Equal(3, vector.Count);
            Assert.Equal(5, size);

            return Task.FromResult(hybridDocuments ?? titleDocuments);
        }
    }

    private sealed class StubEmbeddingClient(IReadOnlyList<float> vector) : IAzureOpenAiEmbeddingClient
    {
        public Task<EmbeddingVector> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
        {
            Assert.Equal("how does the api gateway secure service-to-service calls to the search indexing pipeline", input);

            return Task.FromResult(new EmbeddingVector(vector));
        }
    }
}