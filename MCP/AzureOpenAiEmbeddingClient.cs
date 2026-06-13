using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MCP;

public sealed class AzureOpenAiEmbeddingsOptions
{
    public const string SectionName = "AzureOpenAI";

    public string? Endpoint { get; init; }

    public string? ApiKey { get; init; }

    public string? EmbeddingDeployment { get; init; }

    public string ApiVersion { get; init; } = "2024-02-01";
}

public sealed record EmbeddingVector(IReadOnlyList<float> Vector);

public interface IAzureOpenAiEmbeddingClient
{
    Task<EmbeddingVector> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default);
}

public sealed class AzureOpenAiEmbeddingClient(HttpClient httpClient, IOptions<AzureOpenAiEmbeddingsOptions> options) : IAzureOpenAiEmbeddingClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly AzureOpenAiEmbeddingsOptions _options = options.Value;

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new InvalidOperationException("Embedding input is required.");
        }

        ValidateOptions(_options);
        ConfigureClient();

        using var response = await _httpClient.PostAsJsonAsync(BuildRequestUri(), new { input }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AzureOpenAiEmbeddingResponse>(cancellationToken: cancellationToken);
        var vector = payload?.Data?.FirstOrDefault()?.Embedding;

        if (vector is null || vector.Count == 0)
        {
            throw new HttpRequestException("Azure OpenAI returned an empty embedding vector.");
        }

        return new EmbeddingVector(vector);
    }

    private void ConfigureClient()
    {
        _httpClient.BaseAddress = new Uri(AppendTrailingSlash(_options.Endpoint!));
        _httpClient.DefaultRequestHeaders.Remove("api-key");
        _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey!);
    }

    private string BuildRequestUri()
    {
        return $"openai/deployments/{Uri.EscapeDataString(_options.EmbeddingDeployment!)}/embeddings?api-version={Uri.EscapeDataString(_options.ApiVersion)}";
    }

    private static string AppendTrailingSlash(string url)
    {
        return url.EndsWith("/", StringComparison.Ordinal) ? url : $"{url}/";
    }

    private static void ValidateOptions(AzureOpenAiEmbeddingsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.EmbeddingDeployment))
        {
            throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment is not configured.");
        }
    }

    private sealed record AzureOpenAiEmbeddingResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<AzureOpenAiEmbeddingData>? Data);

    private sealed record AzureOpenAiEmbeddingData(
        [property: JsonPropertyName("embedding")] IReadOnlyList<float> Embedding);
}