# MCP Server Demo

This repo contains a custom ASP.NET Core MCP-style server with HTTP endpoints for health, embeddings, Azure AI Search, Azure DevOps work item lookup, and an MCP JSON-RPC endpoint at `/mcp`.

## Local Run

Prerequisites:
- .NET 10 SDK
- Network access to Azure services if you want live search, embeddings, or Azure DevOps results

Run the server from the repo root:

```powershell
cd C:\Users\mdussa\Desktop\WOW-Team1\Int\Interview\MCP
dotnet run --project .\MCP.csproj
```

Default local URLs:
- `http://localhost:5007`
- `https://localhost:7236`

Useful local endpoints:
- `GET /`
- `GET /health`
- `GET /mcp/info`
- `GET /openapi/v1.json` in Development
- `POST /mcp`

## Azure Setup Checklist

Fill these values in `appsettings.json`, `appsettings.Development.json`, or user secrets/environment variables before running live Azure demos.

Azure DevOps:
- `AzureDevOps:OrganizationUrl`: Azure DevOps org URL such as `https://dev.azure.com/your-org`
- `AzureDevOps:Project`: project name that contains the work item
- `AzureDevOps:Pat`: PAT with read access to work items
- `AzureDevOps:ApiVersion`: keep `7.1` unless your org requires a different supported API version

Azure AI Search:
- `AzureSearch:Endpoint`: search service URL such as `https://your-search.search.windows.net`
- `AzureSearch:ApiKey`: admin or query key with access to the index
- `AzureSearch:IndexName`: index containing the architecture documents
- `AzureSearch:TitleField`: title field name, default `title`
- `AzureSearch:ContentField`: content field name, default `content`
- `AzureSearch:VectorField`: vector field name, default `contentVector`
- `AzureSearch:MaxResults`: max matches returned per request

Azure OpenAI embeddings:
- `AzureOpenAI:Endpoint`: resource URL such as `https://your-openai-resource.openai.azure.com`
- `AzureOpenAI:ApiKey`: API key for the Azure OpenAI resource
- `AzureOpenAI:EmbeddingDeployment`: embedding deployment name used for hybrid search
- `AzureOpenAI:ApiVersion`: keep `2024-02-01` unless you intentionally move to another supported API version

Validation checklist before a live demo:
- `GET /health` returns 200
- `POST /embeddings` returns a vector
- `GET /azure-search/architecture-documents?query=architecture` returns titles
- `GET /azure-search/architecture-documents/hybrid?query=...` returns passages
- `GET /azure-devops/workitems/{id}` returns a real work item
- `POST /mcp` with `tools/list` returns all tools
- `POST /mcp` with `tools/call` succeeds for search and DevOps tools

## Demo Scenarios

### Zero-config meeting demo

This demo proves the server is running and the MCP contract works even before Azure credentials are added.

1. Start the server with `dotnet run --project .\MCP.csproj`.
2. Open `DEMO.http` in VS Code.
3. Run these requests in order:
   - `GET /health`
   - `POST /mcp` initialize
   - `POST /mcp` tools/list
   - `POST /mcp` tools/call for `health_check`
4. Optional: run `tools/call` for `architecture_search_hybrid` to show graceful configuration errors.

### Full Azure-backed server demo

This demo shows the real value of the server after config is supplied.

1. Populate the Azure settings listed above.
2. Start the server.
3. In `DEMO.http`, run:
   - `POST /embeddings`
   - `GET /azure-search/architecture-documents`
   - `GET /azure-search/architecture-documents/hybrid`
   - `GET /azure-devops/workitems/{id}`
   - `POST /mcp` tools/call for `architecture_search_hybrid`
   - `POST /mcp` tools/call for `azure_devops_get_work_item`

### MCP consumer demo with Semantic Kernel host

This shows a client importing the MCP tools and using them in a simple workflow.

List imported MCP tools:

```powershell
dotnet run --project .\MCP.SemanticKernelHost\MCP.SemanticKernelHost.csproj -- http://localhost:5007/mcp
```

Run the workflow demo:

```powershell
dotnet run --project .\MCP.SemanticKernelHost\MCP.SemanticKernelHost.csproj -- workflow "We need a better secure architecture indexing experience. Please check work item 108 and the related gateway/search pipeline docs." http://localhost:5007/mcp
```

Expected outcome:
- Console logs show MCP-backed tool invocation
- Transcript shows research and backlog lookup steps
- With Azure config missing, you will see clear configuration messages
- With Azure config present, you will see live search and work item data

## Demo Files

- `DEMO.http`: minimal meeting script with the exact HTTP requests
- `MCP.http`: broader scratch file for manual validation
