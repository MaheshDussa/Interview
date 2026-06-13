using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.McpServer;

[McpServerToolType]
public static class Lab6Tools
{
    [McpServerTool, Description("Builds a single daily developer summary from Azure DevOps, including assigned work, blocked tasks, pending PR reviews, failed builds, and upcoming releases.")]
    public static async Task<string> developer_daily_assistant(
        [Description("Optional assignee display name or email. Defaults to AZDO_DEFAULT_ASSIGNEE, AZDO_ASSIGNED_TO, or @Me.")] string? assignedTo = null,
        [Description("Optional repository name used to check pending PR reviews. Defaults to AZDO_REPOSITORY when configured.")] string? repositoryName = null,
        [Description("How many days of builds and releases to consider for the daily summary.")] int daysBack = 3)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        try
        {
            return await client!.DeveloperDailyAssistantAsync(assignedTo, repositoryName, daysBack);
        }
        catch (Exception exception)
        {
            return $"The developer daily assistant tool failed: {exception.Message}";
        }
    }
}