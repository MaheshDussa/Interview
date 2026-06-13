using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.McpServer;

[McpServerToolType]
public static class Lab3Tools
{
    [McpServerTool, Description("Creates a smart Azure Boards work-item bundle for a feature request, including a parent item and linked delivery tasks.")]
    public static async Task<string> smart_work_item_creator(
        [Description("The feature title, for example 'Implement Forgot Password feature'.")] string featureTitle,
        [Description("Optional feature description or acceptance criteria.")] string? featureDescription = null,
        [Description("Set to true to create the items in Azure Boards. When false, the tool returns a preview only.")] bool createInAzureDevOps = false,
        [Description("Optional existing parent work item ID. If provided, the generated tasks are linked to this parent instead of creating a new one.")] int? parentWorkItemId = null,
        [Description("Optional UI technology label used in the generated task titles. Defaults to Angular.")] string? uiTechnology = null,
        [Description("Optional preferred parent work item type. Examples: User Story, Product Backlog Item, Feature.")] string? parentWorkItemType = null,
        [Description("Task bundle template. Examples: fullstack, api, ui, integration, data, security, bugfix.")] string? featureType = null,
        [Description("Optional comma-separated or semicolon-separated tags to apply automatically.")] string? tags = null,
        [Description("Optional Azure Boards area path to set on the parent and child tasks.")] string? areaPath = null,
        [Description("Optional Azure Boards iteration path to set on the parent and child tasks.")] string? iterationPath = null,
        [Description("Optional assignee to apply automatically to the parent and child tasks.")] string? assignee = null)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        try
        {
            return await client!.CreateSmartWorkItemBundleAsync(
                featureTitle,
                featureDescription,
                createInAzureDevOps,
                parentWorkItemId,
                uiTechnology,
                parentWorkItemType,
                featureType,
                tags,
                areaPath,
                iterationPath,
                assignee);
        }
        catch (Exception exception)
        {
            return $"The smart work item creator failed: {exception.Message}";
        }
    }
}