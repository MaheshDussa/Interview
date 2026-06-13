using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.AIQuality.McpServer;

[McpServerToolType]
public static class QualityTools
{
    [McpServerTool, Description("Reads SonarQube issues, quality gate status, and security hotspots for a project and returns a quality overview.")]
    public static async Task<string> sonarqube_quality_overview(
        [Description("The SonarQube project key.")] string projectKey,
        [Description("Optional branch name.")] string? branch = null,
        [Description("Maximum number of issues to include in the output.")] int top = 15)
    {
        if (!SonarQubeQualityClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(
            () => client!.GetQualityOverviewAsync(projectKey, branch, top),
            "The SonarQube quality overview tool failed");
    }

    [McpServerTool, Description("Reads Application Insights exceptions, request failures, and dependency failures for the requested time window.")]
    public static async Task<string> app_insights_failure_summary(
        [Description("How many days of telemetry to inspect.")] int daysBack = 7,
        [Description("Optional cloud role name filter.")] string? cloudRoleName = null,
        [Description("Maximum number of exception groups to include.")] int top = 10)
    {
        if (!ApplicationInsightsQualityClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(
            () => client!.GetFailureSummaryAsync(daysBack, cloudRoleName, top),
            "The Application Insights failure summary tool failed");
    }

    [McpServerTool, Description("Reads Azure DevOps repository and pull request metadata to summarize repository intelligence for architecture and review workflows.")]
    public static async Task<string> repository_intelligence(
        [Description("Optional repository name. Defaults to AZDO_REPOSITORY when configured.")] string? repositoryName = null,
        [Description("Optional pull request ID for deeper context.")] int? pullRequestId = null)
    {
        if (!AzureDevOpsQualityClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        return await ExecuteSafeAsync(
            () => client!.GetRepositoryIntelligenceAsync(repositoryName, pullRequestId),
            "The repository intelligence tool failed");
    }

    [McpServerTool, Description("Correlates SonarQube findings, Application Insights failures, and Azure DevOps repository context into a root-cause report.")]
    public static async Task<string> root_cause_analysis(
        [Description("The SonarQube project key.")] string projectKey,
        [Description("Optional Azure DevOps repository name. Defaults to AZDO_REPOSITORY when configured.")] string? repositoryName = null,
        [Description("Optional exception keyword such as NullReferenceException.")] string? exceptionKeyword = null,
        [Description("How many days of runtime telemetry to inspect.")] int daysBack = 7,
        [Description("Optional pull request ID to correlate against changed files.")] int? pullRequestId = null)
    {
        if (!SonarQubeQualityClient.TryCreate(out var sonarClient, out var sonarConfigurationMessage))
        {
            return sonarConfigurationMessage;
        }

        if (!ApplicationInsightsQualityClient.TryCreate(out var appInsightsClient, out var appInsightsConfigurationMessage))
        {
            return appInsightsConfigurationMessage;
        }

        if (!AzureDevOpsQualityClient.TryCreate(out var azureDevOpsClient, out var azureDevOpsConfigurationMessage))
        {
            return azureDevOpsConfigurationMessage;
        }

        return await ExecuteSafeAsync(
            () => QualityCorrelationService.BuildRootCauseAnalysisAsync(
                sonarClient!,
                appInsightsClient!,
                azureDevOpsClient!,
                projectKey,
                repositoryName,
                exceptionKeyword,
                daysBack,
                pullRequestId),
            "The root cause analysis tool failed");
    }

    [McpServerTool, Description("Generates a remediation plan and can optionally create Azure Boards work items for the remediation hierarchy.")]
    public static async Task<string> create_remediation_work_items(
        [Description("Short title for the incident or quality issue.")] string title,
        [Description("Analysis summary or problem statement used to generate the remediation plan.")] string analysisSummary,
        [Description("Set to true to create Azure Boards work items. When false, the tool returns a preview only.")] bool createInAzureDevOps = false,
        [Description("Optional assignee override.")] string? assignee = null,
        [Description("Optional tags to apply to the created work items.")] string? tags = null)
    {
        if (!AzureDevOpsQualityClient.TryCreate(out var client, out var configurationMessage))
        {
            return configurationMessage;
        }

        var plan = QualityCorrelationService.BuildRemediationPlan(title, analysisSummary);
        return await ExecuteSafeAsync(
            () => client!.CreateRemediationWorkItemsAsync(plan, createInAzureDevOps, assignee, tags),
            "The remediation planning tool failed");
    }

    [McpServerTool, Description("Runs root-cause correlation and immediately derives the remediation hierarchy from that structured report, with optional Azure Boards creation.")]
    public static async Task<string> root_cause_to_remediation_work_items(
        [Description("The SonarQube project key.")] string projectKey,
        [Description("Optional Azure DevOps repository name. Defaults to AZDO_REPOSITORY when configured.")] string? repositoryName = null,
        [Description("Optional exception keyword such as NullReferenceException.")] string? exceptionKeyword = null,
        [Description("How many days of runtime telemetry to inspect.")] int daysBack = 7,
        [Description("Optional pull request ID to correlate against changed files.")] int? pullRequestId = null,
        [Description("Set to true to create Azure Boards work items. When false, the tool returns a preview only.")] bool createInAzureDevOps = false,
        [Description("Optional assignee override.")] string? assignee = null,
        [Description("Optional tags to apply to the created work items.")] string? tags = null)
    {
        if (!SonarQubeQualityClient.TryCreate(out var sonarClient, out var sonarConfigurationMessage))
        {
            return sonarConfigurationMessage;
        }

        if (!ApplicationInsightsQualityClient.TryCreate(out var appInsightsClient, out var appInsightsConfigurationMessage))
        {
            return appInsightsConfigurationMessage;
        }

        if (!AzureDevOpsQualityClient.TryCreate(out var azureDevOpsClient, out var azureDevOpsConfigurationMessage))
        {
            return azureDevOpsConfigurationMessage;
        }

        return await ExecuteSafeAsync(async () =>
        {
            var report = await QualityCorrelationService.BuildRootCauseReportAsync(
                sonarClient!,
                appInsightsClient!,
                azureDevOpsClient!,
                projectKey,
                repositoryName,
                exceptionKeyword,
                daysBack,
                pullRequestId);

            var rootCauseText = QualityCorrelationService.FormatRootCauseReport(report);
            var remediationPlan = QualityCorrelationService.BuildRemediationPlan(report);
            var remediationText = await azureDevOpsClient!.CreateRemediationWorkItemsAsync(remediationPlan, createInAzureDevOps, assignee, tags);

            return string.Join(Environment.NewLine, rootCauseText, string.Empty, remediationText);
        }, "The root-cause-to-remediation workflow failed");
    }

    [McpServerTool, Description("Builds an AI-assisted pull request review summary and can optionally post the review comment to Azure DevOps.")]
    public static async Task<string> pr_review_assistant(
        [Description("The Azure DevOps repository name.")] string repositoryName,
        [Description("The pull request ID.")] int pullRequestId,
        [Description("Optional SonarQube project key for quality correlation.")] string? projectKey = null,
        [Description("How many days of runtime telemetry to inspect when adding operational context.")] int daysBack = 7,
        [Description("Set to true to post the generated review comment to Azure DevOps.")] bool postComment = false)
    {
        if (!AzureDevOpsQualityClient.TryCreate(out var azureDevOpsClient, out var azureDevOpsConfigurationMessage))
        {
            return azureDevOpsConfigurationMessage;
        }

        SonarQubeQualityClient? sonarClient = null;
        if (!string.IsNullOrWhiteSpace(projectKey) && SonarQubeQualityClient.TryCreate(out var createdSonarClient, out _))
        {
            sonarClient = createdSonarClient;
        }

        ApplicationInsightsQualityClient? appInsightsClient = null;
        if (ApplicationInsightsQualityClient.TryCreate(out var createdAppInsightsClient, out _))
        {
            appInsightsClient = createdAppInsightsClient;
        }

        return await ExecuteSafeAsync(async () =>
        {
            var review = await QualityCorrelationService.BuildPullRequestReviewAsync(
                azureDevOpsClient!,
                repositoryName,
                pullRequestId,
                sonarClient,
                projectKey,
                appInsightsClient,
                daysBack);

            if (!postComment)
            {
                return review;
            }

            var postResult = await azureDevOpsClient!.PostPullRequestReviewCommentAsync(repositoryName, pullRequestId, review);
            return string.Join(Environment.NewLine, review, string.Empty, postResult);
        }, "The PR review assistant tool failed");
    }

    private static async Task<string> ExecuteSafeAsync(Func<Task<string>> operation, string failurePrefix)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception)
        {
            return $"{failurePrefix}: {exception.Message}";
        }
    }
}