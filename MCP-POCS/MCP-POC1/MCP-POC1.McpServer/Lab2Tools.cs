using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.McpServer;

[McpServerToolType]
public static class Lab2Tools
{
    [McpServerTool, Description("Gets Azure DevOps work items assigned to the current user or a specified assignee.")]
    public static async Task<string> get_my_work(
        [Description("Optional display name or email of the assignee. Defaults to AZDO_ASSIGNED_TO or @Me.")] string? assignedTo = null,
        [Description("Set to true to include closed or completed work items.")] bool includeClosed = false,
        [Description("Maximum number of work items to return.")] int top = 10)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(() => client!.GetMyWorkAsync(assignedTo, includeClosed, top));
    }

    [McpServerTool, Description("Generates a suggested child task breakdown for a user story and can optionally create those tasks in Azure DevOps.")]
    public static async Task<string> create_task_breakdown(
        [Description("The parent User Story or Product Backlog Item ID.")] int userStoryId,
        [Description("Optional story title. If omitted, the tool will try to fetch it from Azure DevOps.")] string? storyTitle = null,
        [Description("Optional story description to improve the suggested breakdown.")] string? storyDescription = null,
        [Description("Number of tasks to suggest or create.")] int taskCount = 5,
        [Description("Set to true to create the generated tasks in Azure DevOps.")] bool createInAzureDevOps = false)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(() => client!.CreateTaskBreakdownAsync(userStoryId, storyTitle, storyDescription, taskCount, createInAzureDevOps));
    }

    [McpServerTool, Description("Summarizes the current sprint for a given team, including completion and item breakdowns.")]
    public static async Task<string> sprint_summary(
        [Description("Optional Azure DevOps team name. Defaults to AZDO_TEAM or the project name.")] string? team = null)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(() => client!.SprintSummaryAsync(team));
    }

    [McpServerTool, Description("Generates release notes from recently completed stories and bug fixes.")]
    public static async Task<string> release_notes(
        [Description("How many days of completed work to inspect.")] int daysBack = 7,
        [Description("Optional iteration path to limit the release notes scope.")] string? iterationPath = null)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(() => client!.ReleaseNotesAsync(daysBack, iterationPath));
    }

    [McpServerTool, Description("Checks recent pipeline execution health and optionally includes Application Insights request failure data.")]
    public static async Task<string> deployment_health(
        [Description("How many days of pipeline history to inspect.")] int daysBack = 1,
        [Description("Optional pipeline name filter.")] string? pipelineName = null)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(() => client!.DeploymentHealthAsync(daysBack, pipelineName));
    }

    [McpServerTool, Description("Analyzes recent bugs, filters by module keyword when provided, and highlights recurring issues.")]
    public static async Task<string> bug_analyzer(
        [Description("Optional module, area, title, or tag keyword to filter bugs.")] string? moduleKeyword = null,
        [Description("How many days of bug history to inspect.")] int daysBack = 30)
    {
        if (!AzureDevOpsMcpClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(() => client!.BugAnalyzerAsync(moduleKeyword, daysBack));
    }

    private static async Task<string> ExecuteSafeAsync(Func<Task<string>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception)
        {
            return $"The Azure DevOps MCP tool failed: {exception.Message}";
        }
    }
}