using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.McpServer;

[McpServerToolType]
public static class Lab4Tools
{
    [McpServerTool, Description("Shows code-to-work-item traceability by either listing work items linked to a pull request or code artifacts linked to a work item.")]
    public static async Task<string> code_to_work_item_traceability(
        [Description("Optional repository name. Required for pull request traceability unless AZDO_REPOSITORY is configured.")] string? repositoryName = null,
        [Description("Optional pull request ID to inspect for linked work items.")] int? pullRequestId = null,
        [Description("Optional work item ID to inspect for linked code artifacts.")] int? workItemId = null)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        try
        {
            return await client!.GetCodeTraceabilityAsync(repositoryName, pullRequestId, workItemId);
        }
        catch (Exception exception)
        {
            return $"The code-to-work-item traceability tool failed: {exception.Message}";
        }
    }
}