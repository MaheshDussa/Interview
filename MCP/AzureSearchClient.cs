using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;

namespace MCP;

public sealed class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public string? IndexName { get; init; }

    public string TitleField { get; init; } = "title";

    public string ContentField { get; init; } = "content";

    public string VectorField { get; init; } = "contentVector";

    public int MaxResults { get; init; } = 5;
}

public sealed record SearchDocumentTitle(string Title);

public sealed record ArchitectureParagraphMatch(string Title, string Paragraph);

public interface IArchitectureDocumentSearchClient
{
    Task<IReadOnlyList<SearchDocumentTitle>> SearchTitlesAsync(string keyword, CancellationToken cancellationToken = default);
}

public interface IHybridArchitectureDocumentSearchClient
{
    Task<IReadOnlyList<ArchitectureParagraphMatch>> SearchParagraphsAsync(string query, CancellationToken cancellationToken = default);
}

internal interface IAzureSearchQueryExecutor
{
    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SearchAsync(string keyword, int size, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SearchHybridAsync(string keyword, IReadOnlyList<float> vector, int size, CancellationToken cancellationToken = default);
}

internal sealed class ArchitectureDocumentSearchClient(IAzureSearchQueryExecutor queryExecutor, IOptions<AzureSearchOptions> options) : IArchitectureDocumentSearchClient
{
    private readonly IAzureSearchQueryExecutor _queryExecutor = queryExecutor;
    private readonly AzureSearchOptions _options = options.Value;

    public async Task<IReadOnlyList<SearchDocumentTitle>> SearchTitlesAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new InvalidOperationException("A search keyword is required.");
        }

        var documents = await _queryExecutor.SearchAsync(keyword, _options.MaxResults, cancellationToken);

        return documents
            .Select(document => document.TryGetValue(_options.TitleField, out var titleValue) ? titleValue?.ToString() : null)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => new SearchDocumentTitle(title!))
            .ToArray();
    }
}

internal sealed class HybridArchitectureDocumentSearchClient(
    IAzureSearchQueryExecutor queryExecutor,
    IAzureOpenAiEmbeddingClient embeddingClient,
    IOptions<AzureSearchOptions> options) : IHybridArchitectureDocumentSearchClient
{
    private readonly IAzureSearchQueryExecutor _queryExecutor = queryExecutor;
    private readonly IAzureOpenAiEmbeddingClient _embeddingClient = embeddingClient;
    private readonly AzureSearchOptions _options = options.Value;

    public async Task<IReadOnlyList<ArchitectureParagraphMatch>> SearchParagraphsAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException("A hybrid search query is required.");
        }

        var embedding = await _embeddingClient.GenerateEmbeddingAsync(query, cancellationToken);
        var documents = await _queryExecutor.SearchHybridAsync(query, embedding.Vector, _options.MaxResults, cancellationToken);

        return documents
            .Select(document => new
            {
                Title = document.TryGetValue(_options.TitleField, out var titleValue) ? titleValue?.ToString() : null,
                Content = document.TryGetValue(_options.ContentField, out var contentValue) ? contentValue?.ToString() : null
            })
            .Where(result => !string.IsNullOrWhiteSpace(result.Title) && !string.IsNullOrWhiteSpace(result.Content))
            .Select(result => new ArchitectureParagraphMatch(result.Title!, SelectBestParagraph(result.Content!, query)))
            .ToArray();
    }

    private static string SelectBestParagraph(string content, string query)
    {
        var paragraphs = content
            .Split(["\r\n\r\n", "\n\n", "\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(paragraph => !string.IsNullOrWhiteSpace(paragraph))
            .ToArray();

        if (paragraphs.Length <= 1)
        {
            return content;
        }

        var queryTerms = query
            .Split([' ', ',', '.', ':', ';', '-', '?', '!'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(term => term.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return paragraphs
            .OrderByDescending(paragraph => queryTerms.Count(term => paragraph.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(paragraph => paragraph.Length)
            .First();
    }
}

internal sealed class AzureSearchQueryExecutor(IOptions<AzureSearchOptions> options) : IAzureSearchQueryExecutor
{
    private readonly AzureSearchOptions _options = options.Value;

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SearchAsync(string keyword, int size, CancellationToken cancellationToken = default)
    {
        ValidateOptions(_options);

        var client = new SearchClient(new Uri(_options.Endpoint!), _options.IndexName!, new AzureKeyCredential(_options.ApiKey!));
        var searchOptions = new SearchOptions
        {
            Size = size
        };
        searchOptions.Select.Add(_options.TitleField);

        var response = await client.SearchAsync<SearchDocument>(keyword, searchOptions, cancellationToken);
        var documents = new List<IReadOnlyDictionary<string, object?>>();

        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            documents.Add(new Dictionary<string, object?>(result.Document, StringComparer.OrdinalIgnoreCase));
        }

        return documents;
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> SearchHybridAsync(string keyword, IReadOnlyList<float> vector, int size, CancellationToken cancellationToken = default)
    {
        ValidateOptions(_options);

        if (vector.Count == 0)
        {
            throw new InvalidOperationException("A vector embedding is required for hybrid search.");
        }

        var client = new SearchClient(new Uri(_options.Endpoint!), _options.IndexName!, new AzureKeyCredential(_options.ApiKey!));
        var searchOptions = new SearchOptions
        {
            Size = size,
            VectorSearch = new VectorSearchOptions()
        };
        searchOptions.Select.Add(_options.TitleField);
        searchOptions.Select.Add(_options.ContentField);

        var vectorQuery = new VectorizedQuery(vector.ToArray())
        {
            KNearestNeighborsCount = size
        };
        vectorQuery.Fields.Add(_options.VectorField);
        searchOptions.VectorSearch.Queries.Add(vectorQuery);

        var response = await client.SearchAsync<SearchDocument>(keyword, searchOptions, cancellationToken);
        var documents = new List<IReadOnlyDictionary<string, object?>>();

        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            documents.Add(new Dictionary<string, object?>(result.Document, StringComparer.OrdinalIgnoreCase));
        }

        return documents;
    }

    private static void ValidateOptions(AzureSearchOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException("AzureSearch:Endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("AzureSearch:ApiKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.IndexName))
        {
            throw new InvalidOperationException("AzureSearch:IndexName is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.VectorField))
        {
            throw new InvalidOperationException("AzureSearch:VectorField is not configured.");
        }
    }
}