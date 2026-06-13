using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MCP;

public sealed class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";

    public string? OrganizationUrl { get; init; }

    public string? Project { get; init; }

    public string? Pat { get; init; }

    public string ApiVersion { get; init; } = "7.1";
}

public sealed record AzureDevOpsWorkItem(int Id, string? Title, string? Description);

public interface IAzureDevOpsWorkItemClient
{
    Task<AzureDevOpsWorkItem> GetWorkItemAsync(int workItemId, CancellationToken cancellationToken = default);
}

public sealed class AzureDevOpsWorkItemClient(HttpClient httpClient, IOptions<AzureDevOpsOptions> options) : IAzureDevOpsWorkItemClient
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly AzureDevOpsOptions _options = options.Value;

    public async Task<AzureDevOpsWorkItem> GetWorkItemAsync(int workItemId, CancellationToken cancellationToken = default)
    {
        ValidateOptions(_options);

        ConfigureClient();

        var requestUri = $"{Uri.EscapeDataString(_options.Project!)}/_apis/wit/workitems/{workItemId}?fields=System.Title,System.Description&api-version={Uri.EscapeDataString(_options.ApiVersion)}";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AzureDevOpsWorkItemResponse>(cancellationToken: cancellationToken);
        if (payload is null)
        {
            throw new HttpRequestException("Azure DevOps returned an empty response.");
        }

        return new AzureDevOpsWorkItem(
            payload.Id,
            payload.Fields.TryGetValue("System.Title", out var title) ? title : null,
            payload.Fields.TryGetValue("System.Description", out var description) ? description : null);
    }

    private void ConfigureClient()
    {
        _httpClient.BaseAddress = new Uri(AppendTrailingSlash(_options.OrganizationUrl!));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", EncodePat(_options.Pat!));
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static string EncodePat(string pat)
    {
        return Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
    }

    private static string AppendTrailingSlash(string url)
    {
        return url.EndsWith("/", StringComparison.Ordinal) ? url : $"{url}/";
    }

    private static void ValidateOptions(AzureDevOpsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OrganizationUrl))
        {
            throw new InvalidOperationException("AzureDevOps:OrganizationUrl is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Project))
        {
            throw new InvalidOperationException("AzureDevOps:Project is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Pat))
        {
            throw new InvalidOperationException("AzureDevOps:Pat is not configured.");
        }
    }

    private sealed record AzureDevOpsWorkItemResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("fields")] IReadOnlyDictionary<string, string> Fields);
}