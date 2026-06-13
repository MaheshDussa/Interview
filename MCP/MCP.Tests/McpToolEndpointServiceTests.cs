using System.Text.Json.Nodes;
using MCP;

namespace MCP.Tests;

public sealed class McpToolEndpointServiceTests
{
    [Fact]
    public async Task CallToolAsync_ExecutesArchitectureSearchTool()
    {
        var service = new McpToolEndpointService(
            new StubArchitectureDocumentSearchClient([new SearchDocumentTitle("Gateway Security Design")]),
            new StubHybridArchitectureDocumentSearchClient([]),
            new StubAzureDevOpsWorkItemClient(new AzureDevOpsWorkItem(42, "Unused", "Unused")));

        var result = await service.CallToolAsync("architecture_search_titles", new JsonObject
        {
            ["query"] = "gateway security"
        });

        Assert.False(result.IsError);
        Assert.Contains("Gateway Security Design", result.Content[0].Text);
    }

    [Fact]
    public async Task CallToolAsync_ExecutesAzureDevOpsTool()
    {
        var service = new McpToolEndpointService(
            new StubArchitectureDocumentSearchClient([]),
            new StubHybridArchitectureDocumentSearchClient([]),
            new StubAzureDevOpsWorkItemClient(new AzureDevOpsWorkItem(108, "Indexing pipeline failure", "Description from Azure DevOps")));

        var result = await service.CallToolAsync("azure_devops_get_work_item", new JsonObject
        {
            ["workItemId"] = 108
        });

        Assert.False(result.IsError);
        Assert.Contains("Indexing pipeline failure", result.Content[0].Text);
        Assert.Contains("Description from Azure DevOps", result.Content[0].Text);
    }

    [Fact]
    public void ListTools_IncludesMappedSearchAndAzureDevOpsTools()
    {
        var service = new McpToolEndpointService(
            new StubArchitectureDocumentSearchClient([]),
            new StubHybridArchitectureDocumentSearchClient([]),
            new StubAzureDevOpsWorkItemClient(new AzureDevOpsWorkItem(1, "Title", "Description")));

        var tools = service.ListTools();

        Assert.Contains(tools, tool => tool.Name == "architecture_search_titles");
        Assert.Contains(tools, tool => tool.Name == "architecture_search_hybrid");
        Assert.Contains(tools, tool => tool.Name == "azure_devops_get_work_item");
    }

    private sealed class StubArchitectureDocumentSearchClient(IReadOnlyList<SearchDocumentTitle> titles) : IArchitectureDocumentSearchClient
    {
        public Task<IReadOnlyList<SearchDocumentTitle>> SearchTitlesAsync(string keyword, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(titles);
        }
    }

    private sealed class StubHybridArchitectureDocumentSearchClient(IReadOnlyList<ArchitectureParagraphMatch> matches) : IHybridArchitectureDocumentSearchClient
    {
        public Task<IReadOnlyList<ArchitectureParagraphMatch>> SearchParagraphsAsync(string query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(matches);
        }
    }

    private sealed class StubAzureDevOpsWorkItemClient(AzureDevOpsWorkItem workItem) : IAzureDevOpsWorkItemClient
    {
        public Task<AzureDevOpsWorkItem> GetWorkItemAsync(int workItemId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(workItem);
        }
    }
}