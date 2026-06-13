using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Azure;
using MCP;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddOptions<AzureDevOpsOptions>()
    .Bind(builder.Configuration.GetSection(AzureDevOpsOptions.SectionName));
builder.Services.AddOptions<AzureSearchOptions>()
    .Bind(builder.Configuration.GetSection(AzureSearchOptions.SectionName));
builder.Services.AddOptions<AzureOpenAiEmbeddingsOptions>()
    .Bind(builder.Configuration.GetSection(AzureOpenAiEmbeddingsOptions.SectionName));
builder.Services.AddHttpClient<IAzureDevOpsWorkItemClient, AzureDevOpsWorkItemClient>();
builder.Services.AddHttpClient<IAzureOpenAiEmbeddingClient, AzureOpenAiEmbeddingClient>();
builder.Services.AddScoped<IAzureSearchQueryExecutor, AzureSearchQueryExecutor>();
builder.Services.AddScoped<IArchitectureDocumentSearchClient, ArchitectureDocumentSearchClient>();
builder.Services.AddScoped<IHybridArchitectureDocumentSearchClient, HybridArchitectureDocumentSearchClient>();
builder.Services.AddScoped<IMcpToolEndpointService, McpToolEndpointService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "Custom MCP Server",
    description = "ASP.NET Core Minimal API starter for a custom Model Context Protocol service.",
    openApi = "/openapi/v1.json",
    health = "/health"
}))
.WithName("GetServiceInfo");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "Custom MCP Server",
    utcTimestamp = DateTimeOffset.UtcNow
}))
.WithName("GetHealth");

app.MapGet("/mcp/info", () => Results.Ok(new
{
    server = "Custom MCP Server",
    protocol = "Model Context Protocol",
    transport = "HTTP",
    version = "1.0.0-preview"
}))
.WithName("GetMcpInfo");

app.MapGet("/azure-devops/workitems/{id:int}", async (int id, IAzureDevOpsWorkItemClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var workItem = await client.GetWorkItemAsync(id, cancellationToken);

        return Results.Ok(new
        {
            workItem.Id,
            workItem.Title,
            workItem.Description
        });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("GetAzureDevOpsWorkItem");

app.MapGet("/azure-search/architecture-documents", async (string query, IArchitectureDocumentSearchClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var results = await client.SearchTitlesAsync(query, cancellationToken);

        return Results.Ok(new
        {
            query,
            titles = results.Select(result => result.Title).ToArray()
        });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (RequestFailedException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("SearchArchitectureDocuments");

app.MapGet("/azure-search/architecture-documents/hybrid", async (string query, IHybridArchitectureDocumentSearchClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var results = await client.SearchParagraphsAsync(query, cancellationToken);

        return Results.Ok(new
        {
            query,
            matches = results.Select(result => new
            {
                result.Title,
                result.Paragraph
            }).ToArray()
        });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (RequestFailedException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("HybridSearchArchitectureDocuments");

app.MapPost("/embeddings", async (EmbeddingRequest request, IAzureOpenAiEmbeddingClient client, CancellationToken cancellationToken) =>
{
    try
    {
        var embedding = await client.GenerateEmbeddingAsync(request.Input, cancellationToken);

        return Results.Ok(new
        {
            request.Input,
            vector = embedding.Vector
        });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (HttpRequestException exception)
    {
        return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("GenerateEmbeddings");

app.MapPost("/mcp", async (HttpRequest request, IMcpToolEndpointService toolService, CancellationToken cancellationToken) =>
{
    JsonRpcRequest? rpcRequest;

    try
    {
        rpcRequest = await request.ReadFromJsonAsync<JsonRpcRequest>();
    }
    catch (System.Text.Json.JsonException)
    {
        return Results.BadRequest(new JsonRpcResponse
        {
            Id = null,
            Error = new JsonRpcError(-32700, "Parse error")
        });
    }

    if (rpcRequest is null || rpcRequest.JsonRpc != "2.0" || string.IsNullOrWhiteSpace(rpcRequest.Method))
    {
        return Results.BadRequest(new JsonRpcResponse
        {
            Id = rpcRequest?.Id,
            Error = new JsonRpcError(-32600, "Invalid Request")
        });
    }

    if (string.Equals(rpcRequest.Method, "initialize", StringComparison.Ordinal))
    {
        var requestedProtocolVersion = rpcRequest.Params?["protocolVersion"]?.GetValue<string>();

        return Results.Ok(new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = new InitializeResult(
                requestedProtocolVersion ?? "2024-11-05",
                new ServerCapabilities(new ToolsCapabilities(false)),
                new ServerInfo("Custom MCP Server", "1.0.0"))
        });
    }

    if (string.Equals(rpcRequest.Method, "initialized", StringComparison.Ordinal)
        || string.Equals(rpcRequest.Method, "notifications/initialized", StringComparison.Ordinal))
    {
        if (rpcRequest.Id is null)
        {
            return Results.NoContent();
        }

        return Results.Ok(new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = new { acknowledged = true }
        });
    }

    if (string.Equals(rpcRequest.Method, "tools/list", StringComparison.Ordinal))
    {
        return Results.Ok(new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = new ToolsListResult(toolService.ListTools())
        });
    }

    if (string.Equals(rpcRequest.Method, "tools/call", StringComparison.Ordinal))
    {
        var toolName = rpcRequest.Params?["name"]?.GetValue<string>();
        var arguments = rpcRequest.Params?["arguments"] as JsonObject;

        if (string.IsNullOrWhiteSpace(toolName))
        {
            return Results.BadRequest(new JsonRpcResponse
            {
                Id = rpcRequest.Id,
                Error = new JsonRpcError(-32602, "Tool name is required.")
            });
        }

        var result = await toolService.CallToolAsync(toolName, arguments, cancellationToken);

        return Results.Ok(new JsonRpcResponse
        {
            Id = rpcRequest.Id,
            Result = result
        });
    }

    return Results.Ok(new JsonRpcResponse
    {
        Id = rpcRequest.Id,
        Error = new JsonRpcError(-32601, $"Method '{rpcRequest.Method}' not found")
    });
})
.WithName("HandleMcpJsonRpc");

app.Run();

sealed record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string? JsonRpc,
    [property: JsonPropertyName("id")] JsonNode? Id,
    [property: JsonPropertyName("method")] string? Method,
    [property: JsonPropertyName("params")] JsonObject? Params);

sealed record JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public JsonNode? Id { get; init; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonRpcError? Error { get; init; }
}

sealed record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message);

sealed record InitializeResult(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("capabilities")] ServerCapabilities Capabilities,
    [property: JsonPropertyName("serverInfo")] ServerInfo ServerInfo);

sealed record ServerCapabilities(
    [property: JsonPropertyName("tools")] ToolsCapabilities Tools);

sealed record ToolsCapabilities(
    [property: JsonPropertyName("listChanged")] bool ListChanged);

sealed record ServerInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version);

sealed record ToolsListResult(
    [property: JsonPropertyName("tools")] IReadOnlyList<ToolDefinition> Tools);

sealed record ToolDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("inputSchema")] ToolInputSchema InputSchema);

sealed record ToolInputSchema(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("properties")] JsonObject Properties,
    [property: JsonPropertyName("additionalProperties")] bool AdditionalProperties);

sealed record EmbeddingRequest(string Input);
