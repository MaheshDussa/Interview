using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MCP;

internal interface IMcpToolEndpointService
{
    IReadOnlyList<ToolDefinition> ListTools();

    Task<McpToolCallResult> CallToolAsync(string name, JsonObject? arguments, CancellationToken cancellationToken = default);
}

internal sealed class McpToolEndpointService(
    IArchitectureDocumentSearchClient architectureSearchClient,
    IHybridArchitectureDocumentSearchClient hybridArchitectureSearchClient,
    IAzureDevOpsWorkItemClient azureDevOpsWorkItemClient) : IMcpToolEndpointService
{
    private static readonly IReadOnlyList<ToolDefinition> ToolDefinitions =
    [
        new(
            "health_check",
            "Returns the current service health status.",
            new ToolInputSchema("object", new JsonObject(), false)),
        new(
            "architecture_search_titles",
            "Searches architecture and system documents by keyword and returns matching document titles.",
            BuildSchema(("query", "string", "Keyword query for architecture content."))),
        new(
            "architecture_search_hybrid",
            "Runs hybrid BM25 and vector search and returns the most relevant architecture paragraphs.",
            BuildSchema(("query", "string", "Natural language architecture question."))),
        new(
            "azure_devops_get_work_item",
            "Fetches an Azure DevOps work item and returns its title and description.",
            BuildSchema(("workItemId", "integer", "Azure DevOps work item ID.")))
    ];

    private readonly IArchitectureDocumentSearchClient _architectureSearchClient = architectureSearchClient;
    private readonly IHybridArchitectureDocumentSearchClient _hybridArchitectureSearchClient = hybridArchitectureSearchClient;
    private readonly IAzureDevOpsWorkItemClient _azureDevOpsWorkItemClient = azureDevOpsWorkItemClient;

    public IReadOnlyList<ToolDefinition> ListTools() => ToolDefinitions;

    public async Task<McpToolCallResult> CallToolAsync(string name, JsonObject? arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            return name switch
            {
                "health_check" => BuildSuccessResult(
                    "Service is healthy.",
                    new
                    {
                        status = "Healthy",
                        service = "Custom MCP Server",
                        utcTimestamp = DateTimeOffset.UtcNow
                    }),
                "architecture_search_titles" => await SearchArchitectureTitlesAsync(arguments, cancellationToken),
                "architecture_search_hybrid" => await SearchArchitectureHybridAsync(arguments, cancellationToken),
                "azure_devops_get_work_item" => await GetAzureDevOpsWorkItemAsync(arguments, cancellationToken),
                _ => BuildErrorResult($"Tool '{name}' is not registered.")
            };
        }
        catch (InvalidOperationException exception)
        {
            return BuildErrorResult(exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return BuildErrorResult(exception.Message);
        }
        catch (Azure.RequestFailedException exception)
        {
            return BuildErrorResult(exception.Message);
        }
    }

    private async Task<McpToolCallResult> SearchArchitectureTitlesAsync(JsonObject? arguments, CancellationToken cancellationToken)
    {
        var query = GetRequiredString(arguments, "query");
        var titles = await _architectureSearchClient.SearchTitlesAsync(query, cancellationToken);
        var structuredContent = new
        {
            query,
            titles = titles.Select(item => item.Title).ToArray()
        };

        return BuildSuccessResult(
            titles.Count == 0
                ? $"No architecture titles matched '{query}'."
                : string.Join(Environment.NewLine, titles.Select(item => item.Title)),
            structuredContent);
    }

    private async Task<McpToolCallResult> SearchArchitectureHybridAsync(JsonObject? arguments, CancellationToken cancellationToken)
    {
        var query = GetRequiredString(arguments, "query");
        var matches = await _hybridArchitectureSearchClient.SearchParagraphsAsync(query, cancellationToken);
        var structuredContent = new
        {
            query,
            matches = matches.Select(match => new
            {
                match.Title,
                match.Paragraph
            }).ToArray()
        };

        return BuildSuccessResult(
            matches.Count == 0
                ? $"No hybrid matches found for '{query}'."
                : string.Join(
                    Environment.NewLine + Environment.NewLine,
                    matches.Select(match => $"{match.Title}: {match.Paragraph}")),
            structuredContent);
    }

    private async Task<McpToolCallResult> GetAzureDevOpsWorkItemAsync(JsonObject? arguments, CancellationToken cancellationToken)
    {
        var workItemId = GetRequiredInt(arguments, "workItemId");
        var workItem = await _azureDevOpsWorkItemClient.GetWorkItemAsync(workItemId, cancellationToken);
        var structuredContent = new
        {
            workItem.Id,
            workItem.Title,
            workItem.Description
        };

        return BuildSuccessResult(
            $"{workItem.Title}{Environment.NewLine}{Environment.NewLine}{workItem.Description}",
            structuredContent);
    }

    private static string GetRequiredString(JsonObject? arguments, string propertyName)
    {
        var value = arguments?[propertyName]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Argument '{propertyName}' is required.");
        }

        return value;
    }

    private static int GetRequiredInt(JsonObject? arguments, string propertyName)
    {
        var node = arguments?[propertyName];
        if (node is null)
        {
            throw new InvalidOperationException($"Argument '{propertyName}' is required.");
        }

        try
        {
            return node.GetValue<int>();
        }
        catch (InvalidOperationException)
        {
            throw new InvalidOperationException($"Argument '{propertyName}' is required.");
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"Argument '{propertyName}' is required.");
        }
    }

    private static ToolInputSchema BuildSchema(params (string Name, string Type, string Description)[] properties)
    {
        var schemaProperties = new JsonObject();
        foreach (var property in properties)
        {
            schemaProperties[property.Name] = new JsonObject
            {
                ["type"] = property.Type,
                ["description"] = property.Description
            };
        }

        return new ToolInputSchema("object", schemaProperties, false);
    }

    private static McpToolCallResult BuildSuccessResult(string text, object structuredContent)
    {
        return new McpToolCallResult([new McpTextContent("text", text)], structuredContent, false);
    }

    private static McpToolCallResult BuildErrorResult(string message)
    {
        return new McpToolCallResult([new McpTextContent("text", message)], new { error = message }, true);
    }
}

internal sealed record McpToolCallResult(
    [property: JsonPropertyName("content")] IReadOnlyList<McpTextContent> Content,
    [property: JsonPropertyName("structuredContent")] object StructuredContent,
    [property: JsonPropertyName("isError")] bool IsError);

internal sealed record McpTextContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);