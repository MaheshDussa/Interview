using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.McpServer;

[McpServerToolType]
public static class Lab5Tools
{
    [McpServerTool, Description("Builds an AI sprint health dashboard summary from Azure DevOps sprint data, highlighting completion, delayed work, workload balance, and a simple risk signal.")]
    public static async Task<string> sprint_health_dashboard(
        [Description("Optional Azure DevOps team name. Defaults to AZDO_TEAM or the project name.")] string? team = null,
        [Description("How many days without changes should mark an active item as delayed.")] int delayedAfterDays = 3,
        [Description("How many active items assigned to one developer should count as overloaded.")] int overloadedThreshold = 5)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        try
        {
            return await client!.SprintHealthDashboardAsync(team, delayedAfterDays, overloadedThreshold);
        }
        catch (Exception exception)
        {
            return $"The sprint health dashboard tool failed: {exception.Message}";
        }
    }
}