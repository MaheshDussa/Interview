using MCP_POC1.Shared;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MCP_POC1.McpServer;

internal sealed class AzureDevOpsMcpClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static bool TryCreate(out AzureDevOpsMcpClient? client, out string configurationMessage)
    {
        var organizationUrl = EnvironmentSettings.ReadFirst("AZDO_ORGANIZATION_URL", "AZDO_ORG_URL");
        var project = EnvironmentSettings.ReadFirst("AZDO_PROJECT");
        var personalAccessToken = EnvironmentSettings.ReadFirst("AZDO_PAT");

        var missingSettings = new List<string>();

        if (string.IsNullOrWhiteSpace(organizationUrl))
        {
            missingSettings.Add("AZDO_ORGANIZATION_URL");
        }

        if (string.IsNullOrWhiteSpace(project))
        {
            missingSettings.Add("AZDO_PROJECT");
        }

        if (string.IsNullOrWhiteSpace(personalAccessToken))
        {
            missingSettings.Add("AZDO_PAT");
        }

        if (missingSettings.Count > 0)
        {
            client = null;
            configurationMessage = "Azure DevOps MCP tools are not configured yet. Set these environment variables before calling the Lab 2 tools: "
                + string.Join(", ", missingSettings)
                + ". Optional settings: AZDO_TEAM, AZDO_ASSIGNED_TO, AZDO_DEFAULT_AREA_PATH, AZDO_DEFAULT_ITERATION_PATH, AZDO_DEFAULT_ASSIGNEE, AZDO_DEFAULT_TAGS, AZDO_REPOSITORY, AZDO_REVIEWER_IDENTITY, AZDO_RELEASE_DEFINITION_ID, APPINSIGHTS_APP_ID, APPINSIGHTS_API_KEY.";
            return false;
        }

        client = new AzureDevOpsMcpClient(
            organizationUrl!.TrimEnd('/'),
            project!,
            personalAccessToken!,
            EnvironmentSettings.ReadFirst("AZDO_TEAM"),
            EnvironmentSettings.ReadFirst("AZDO_ASSIGNED_TO"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_AREA_PATH"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_ITERATION_PATH"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_ASSIGNEE"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_TAGS"),
            EnvironmentSettings.ReadFirst("AZDO_REPOSITORY"),
            EnvironmentSettings.ReadFirst("AZDO_REVIEWER_IDENTITY"),
            EnvironmentSettings.ReadFirst("AZDO_RELEASE_DEFINITION_ID"),
            EnvironmentSettings.ReadFirst("APPINSIGHTS_APP_ID"),
            EnvironmentSettings.ReadFirst("APPINSIGHTS_API_KEY"));
        configurationMessage = string.Empty;
        return true;
    }

    private AzureDevOpsMcpClient(
        string organizationUrl,
        string project,
        string personalAccessToken,
        string? team,
        string? assignedTo,
        string? defaultAreaPath,
        string? defaultIterationPath,
        string? defaultAssignee,
        string? defaultTags,
        string? defaultRepository,
        string? defaultReviewerIdentity,
        string? defaultReleaseDefinitionId,
        string? applicationInsightsAppId,
        string? applicationInsightsApiKey)
    {
        OrganizationUrl = organizationUrl;
        Project = project;
        PersonalAccessToken = personalAccessToken;
        Team = string.IsNullOrWhiteSpace(team) ? project : team;
        AssignedTo = assignedTo;
        DefaultAreaPath = defaultAreaPath;
        DefaultIterationPath = defaultIterationPath;
        DefaultAssignee = defaultAssignee;
        DefaultTags = defaultTags;
        DefaultRepository = defaultRepository;
        DefaultReviewerIdentity = defaultReviewerIdentity;
        DefaultReleaseDefinitionId = defaultReleaseDefinitionId;
        ApplicationInsightsAppId = applicationInsightsAppId;
        ApplicationInsightsApiKey = applicationInsightsApiKey;
    }

    public string OrganizationUrl { get; }

    public string Project { get; }

    public string PersonalAccessToken { get; }

    public string Team { get; }

    public string? AssignedTo { get; }

    public string? DefaultAreaPath { get; }

    public string? DefaultIterationPath { get; }

    public string? DefaultAssignee { get; }

    public string? DefaultTags { get; }

    public string? DefaultRepository { get; }

    public string? DefaultReviewerIdentity { get; }

    public string? DefaultReleaseDefinitionId { get; }

    public string? ApplicationInsightsAppId { get; }

    public string? ApplicationInsightsApiKey { get; }

    public async Task<string> GetMyWorkAsync(string? assignedTo, bool includeClosed, int top)
    {
        var resolvedAssignedTo = string.IsNullOrWhiteSpace(assignedTo)
            ? (string.IsNullOrWhiteSpace(AssignedTo) ? "@Me" : AssignedTo!)
            : assignedTo.Trim();

        var wiql = new StringBuilder()
            .Append("SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '")
            .Append(EscapeWiqlValue(Project))
            .Append("' AND [System.AssignedTo] = ")
            .Append(FormatWiqlIdentity(resolvedAssignedTo));

        if (!includeClosed)
        {
            wiql.Append(" AND [System.State] NOT IN ('Closed', 'Done', 'Removed')");
        }

        wiql.Append(" ORDER BY [System.ChangedDate] DESC");

        var workItemIds = await QueryWorkItemIdsAsync(wiql.ToString());
        if (workItemIds.Count == 0)
        {
            return $"No work items were found for {resolvedAssignedTo}.";
        }

        var selectedIds = workItemIds.Take(Math.Max(1, top)).ToArray();
        var workItems = await GetWorkItemsAsync(selectedIds,
            "System.Id",
            "System.Title",
            "System.State",
            "System.WorkItemType",
            "System.IterationPath",
            "System.AssignedTo",
            "System.Tags");

        var lines = new List<string>
        {
            $"Assigned work for {resolvedAssignedTo}:"
        };

        foreach (var workItem in workItems)
        {
            lines.Add($"- #{workItem.Id} [{workItem.WorkItemType}] {workItem.Title} | State: {workItem.State} | Iteration: {workItem.IterationPath}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> CreateTaskBreakdownAsync(int userStoryId, string? storyTitle, string? storyDescription, int taskCount, bool createInAzureDevOps)
    {
        var resolvedStoryTitle = string.IsNullOrWhiteSpace(storyTitle)
            ? await GetWorkItemTitleAsync(userStoryId)
            : storyTitle.Trim();

        if (string.IsNullOrWhiteSpace(resolvedStoryTitle))
        {
            resolvedStoryTitle = $"User Story {userStoryId}";
        }

        var tasks = GenerateTaskBreakdown(resolvedStoryTitle, storyDescription, taskCount);

        if (!createInAzureDevOps)
        {
            var previewLines = new List<string>
            {
                $"Suggested child tasks for story #{userStoryId} ({resolvedStoryTitle}):"
            };

            for (var index = 0; index < tasks.Count; index++)
            {
                previewLines.Add($"{index + 1}. {tasks[index]}");
            }

            previewLines.Add("Set createInAzureDevOps=true to create these tasks in Azure DevOps.");
            return string.Join(Environment.NewLine, previewLines);
        }

        var fieldOptions = ResolveWorkItemFieldOptions(null, null, null, null, "task-breakdown");
        var createdTaskIds = new List<int>();
        foreach (var taskTitle in tasks)
        {
            createdTaskIds.Add(await CreateChildTaskAsync(userStoryId, taskTitle, fieldOptions));
        }

        return $"Created {createdTaskIds.Count} child tasks under story #{userStoryId}: {string.Join(", ", createdTaskIds.Select(id => $"#{id}"))}.";
    }

    public async Task<string> CreateSmartWorkItemBundleAsync(
        string featureTitle,
        string? featureDescription,
        bool createInAzureDevOps,
        int? parentWorkItemId,
        string? uiTechnology,
        string? parentWorkItemType,
        string? featureType,
        string? tags,
        string? areaPath,
        string? iterationPath,
        string? assignee)
    {
        var resolvedFeatureTitle = string.IsNullOrWhiteSpace(featureTitle)
            ? "New feature"
            : featureTitle.Trim();

        var resolvedUiTechnology = string.IsNullOrWhiteSpace(uiTechnology)
            ? "Angular"
            : uiTechnology.Trim();

        var resolvedFeatureType = ResolveFeatureType(featureType);
        var fieldOptions = ResolveWorkItemFieldOptions(tags, areaPath, iterationPath, assignee, resolvedFeatureType);

        var generatedTasks = GenerateSmartFeatureTasks(resolvedFeatureTitle, resolvedUiTechnology, resolvedFeatureType);

        if (!createInAzureDevOps)
        {
            var previewLines = new List<string>
            {
                $"Suggested smart work item bundle for '{resolvedFeatureTitle}':",
                $"Feature type template: {resolvedFeatureType}",
                $"Parent work item type: {ResolvePreferredParentTypes(parentWorkItemType).First()}",
                $"Tags: {fieldOptions.Tags ?? "none"}",
                $"Area path: {fieldOptions.AreaPath ?? "default"}",
                $"Iteration path: {fieldOptions.IterationPath ?? "default"}",
                $"Assignee: {fieldOptions.AssignedTo ?? "default"}",
                "Child tasks:"
            };

            for (var index = 0; index < generatedTasks.Count; index++)
            {
                previewLines.Add($"{index + 1}. {generatedTasks[index]}");
            }

            previewLines.Add("Set createInAzureDevOps=true to create the parent item and link all tasks in Azure Boards.");
            return string.Join(Environment.NewLine, previewLines);
        }

        var createdParentId = parentWorkItemId;
        var resolvedParentType = parentWorkItemType;

        if (!createdParentId.HasValue)
        {
            (createdParentId, resolvedParentType) = await CreateParentWorkItemWithFallbackAsync(
                resolvedFeatureTitle,
                featureDescription,
                parentWorkItemType,
                fieldOptions);
        }

        var createdTaskIds = new List<int>();
        foreach (var taskTitle in generatedTasks)
        {
            createdTaskIds.Add(await CreateChildTaskAsync(createdParentId!.Value, taskTitle, fieldOptions));
        }

        return string.Join(Environment.NewLine,
            $"Created smart work item bundle for '{resolvedFeatureTitle}'.",
            $"Template: {resolvedFeatureType}",
            $"Parent item: #{createdParentId} [{resolvedParentType ?? "Linked Parent"}]",
            $"Child tasks created: {createdTaskIds.Count}",
            string.Join(", ", createdTaskIds.Select(id => $"#{id}")));
    }

    public async Task<string> GetCodeTraceabilityAsync(string? repositoryName, int? pullRequestId, int? workItemId)
    {
        if (pullRequestId.HasValue)
        {
            var resolvedRepository = string.IsNullOrWhiteSpace(repositoryName) ? DefaultRepository : repositoryName.Trim();
            if (string.IsNullOrWhiteSpace(resolvedRepository))
            {
                return "A repositoryName is required for pull request traceability unless AZDO_REPOSITORY is configured.";
            }

            return await GetPullRequestTraceabilityAsync(resolvedRepository, pullRequestId.Value);
        }

        if (workItemId.HasValue)
        {
            return await GetWorkItemTraceabilityAsync(workItemId.Value);
        }

        return "Provide either a pullRequestId or a workItemId to inspect code-to-work-item traceability.";
    }

    public async Task<string> SprintSummaryAsync(string? team)
    {
        var resolvedTeam = string.IsNullOrWhiteSpace(team) ? Team : team.Trim();
        var currentIteration = await GetCurrentIterationPathAsync(resolvedTeam);
        if (string.IsNullOrWhiteSpace(currentIteration))
        {
            return $"No current iteration was found for team '{resolvedTeam}'.";
        }

        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{EscapeWiqlValue(Project)}' AND [System.IterationPath] UNDER '{EscapeWiqlValue(currentIteration)}' ORDER BY [System.ChangedDate] DESC";
        var workItemIds = await QueryWorkItemIdsAsync(wiql);
        if (workItemIds.Count == 0)
        {
            return $"No work items were found in the current sprint '{currentIteration}'.";
        }

        var workItems = await GetWorkItemsAsync(workItemIds,
            "System.Id",
            "System.Title",
            "System.State",
            "System.WorkItemType");

        var total = workItems.Count;
        var completedStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Done", "Closed", "Resolved" };
        var completed = workItems.Count(item => completedStates.Contains(item.State));
        var remaining = total - completed;

        var stateSummary = workItems
            .GroupBy(item => item.State)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}: {group.Count()}");

        var typeSummary = workItems
            .GroupBy(item => item.WorkItemType)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key}: {group.Count()}");

        return string.Join(Environment.NewLine,
            $"Sprint summary for {resolvedTeam}",
            $"Iteration: {currentIteration}",
            $"Total items: {total}",
            $"Completed: {completed}",
            $"Remaining: {remaining}",
            $"Completion: {(total == 0 ? 0 : Math.Round((double)completed / total * 100, 1))}%",
            $"State breakdown: {string.Join(", ", stateSummary)}",
            $"Type breakdown: {string.Join(", ", typeSummary)}");
    }

    public async Task<string> SprintHealthDashboardAsync(string? team, int delayedAfterDays, int overloadedThreshold)
    {
        var resolvedTeam = string.IsNullOrWhiteSpace(team) ? Team : team.Trim();
        var currentIteration = await GetCurrentIterationPathAsync(resolvedTeam);
        if (string.IsNullOrWhiteSpace(currentIteration))
        {
            return $"No current iteration was found for team '{resolvedTeam}'.";
        }

        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{EscapeWiqlValue(Project)}' AND [System.IterationPath] UNDER '{EscapeWiqlValue(currentIteration)}' ORDER BY [System.ChangedDate] DESC";
        var workItemIds = await QueryWorkItemIdsAsync(wiql);
        if (workItemIds.Count == 0)
        {
            return $"No work items were found in the current sprint '{currentIteration}'.";
        }

        var workItems = await GetWorkItemsAsync(workItemIds,
            "System.Id",
            "System.Title",
            "System.State",
            "System.WorkItemType",
            "System.IterationPath",
            "System.AreaPath",
            "System.AssignedTo",
            "System.Tags",
            "System.ChangedDate");

        var completedStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Done", "Closed", "Resolved" };
        var total = workItems.Count;
        var completed = workItems.Count(item => completedStates.Contains(item.State));
        var completionPercent = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1);

        var delayCutoff = DateTime.UtcNow.AddDays(-Math.Abs(delayedAfterDays));
        var delayedTasks = workItems
            .Where(item => !completedStates.Contains(item.State))
            .Where(item => item.ChangedDateUtc.HasValue && item.ChangedDateUtc.Value < delayCutoff)
            .OrderBy(item => item.ChangedDateUtc)
            .Take(10)
            .ToList();

        var activeItemsByDeveloper = workItems
            .Where(item => !completedStates.Contains(item.State))
            .GroupBy(item => string.IsNullOrWhiteSpace(item.AssignedTo) ? "Unassigned" : item.AssignedTo)
            .Select(group => new DeveloperWorkload(group.Key, group.Count()))
            .OrderByDescending(item => item.ActiveItems)
            .ToList();

        var averageActiveItems = activeItemsByDeveloper.Count == 0
            ? 0
            : activeItemsByDeveloper.Average(item => item.ActiveItems);

        var overloadedDevelopers = activeItemsByDeveloper
            .Where(item => item.ActiveItems >= Math.Max(1, overloadedThreshold))
            .ToList();

        var riskLevel = CalculateSprintRisk(completionPercent, delayedTasks.Count, overloadedDevelopers.Count, averageActiveItems);

        var lines = new List<string>
        {
            $"Sprint health dashboard for {resolvedTeam}",
            $"Iteration: {currentIteration}",
            $"Sprint completion: {completionPercent}% ({completed}/{total})",
            $"Delayed tasks: {delayedTasks.Count}",
            $"Developers with heavy workload: {overloadedDevelopers.Count}",
            $"Risk prediction: {riskLevel}"
        };

        lines.Add("Developer workload:");
        if (activeItemsByDeveloper.Count == 0)
        {
            lines.Add("- No active work items found.");
        }
        else
        {
            foreach (var developer in activeItemsByDeveloper.Take(10))
            {
                lines.Add($"- {developer.AssignedTo}: {developer.ActiveItems} active item(s)");
            }
        }

        lines.Add("Delayed tasks:");
        if (delayedTasks.Count == 0)
        {
            lines.Add("- No delayed tasks detected using the current threshold.");
        }
        else
        {
            foreach (var delayedTask in delayedTasks)
            {
                lines.Add($"- #{delayedTask.Id} [{delayedTask.WorkItemType}] {delayedTask.Title} | State: {delayedTask.State} | Assigned: {delayedTask.AssignedTo} | Last changed: {FormatDate(delayedTask.ChangedDateUtc)}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> DeveloperDailyAssistantAsync(string? assignedTo, string? repositoryName, int daysBack)
    {
        var resolvedAssignedTo = string.IsNullOrWhiteSpace(assignedTo)
            ? (string.IsNullOrWhiteSpace(DefaultAssignee) ? (string.IsNullOrWhiteSpace(AssignedTo) ? "@Me" : AssignedTo!) : DefaultAssignee!)
            : assignedTo.Trim();

        var assignedWorkItems = await GetAssignedWorkItemsAsync(resolvedAssignedTo, includeClosed: false, top: 10);
        var blockedItems = assignedWorkItems
            .Where(item => IsBlocked(item))
            .Take(5)
            .ToList();

        var pendingPrReviews = await GetPendingPrReviewsAsync(repositoryName, resolvedAssignedTo, top: 5);
        var failedBuilds = await GetFailedBuildsAsync(daysBack, top: 5);
        var upcomingReleases = await GetUpcomingReleasesAsync(daysBack, top: 5);

        var lines = new List<string>
        {
            $"Developer daily assistant for {resolvedAssignedTo}",
            "Assigned stories and tasks:"
        };

        if (assignedWorkItems.Count == 0)
        {
            lines.Add("- No active assigned work items found.");
        }
        else
        {
            foreach (var workItem in assignedWorkItems)
            {
                lines.Add($"- #{workItem.Id} [{workItem.WorkItemType}] {workItem.Title} | State: {workItem.State}");
            }
        }

        lines.Add("Blocked tasks:");
        if (blockedItems.Count == 0)
        {
            lines.Add("- No blocked tasks detected.");
        }
        else
        {
            foreach (var item in blockedItems)
            {
                lines.Add($"- #{item.Id} {item.Title} | State: {item.State} | Tags: {item.Tags}");
            }
        }

        lines.Add("Pending PR reviews:");
        if (pendingPrReviews.Count == 0)
        {
            lines.Add("- No pending PR reviews found.");
        }
        else
        {
            foreach (var review in pendingPrReviews)
            {
                lines.Add($"- PR #{review.PullRequestId} {review.Title} | Source: {review.SourceBranch} | Target: {review.TargetBranch}");
            }
        }

        lines.Add("Failed builds:");
        if (failedBuilds.Count == 0)
        {
            lines.Add("- No failed builds found in the selected window.");
        }
        else
        {
            foreach (var build in failedBuilds)
            {
                lines.Add($"- {build.DefinitionName} | Result: {build.Result} | Finished: {build.FinishTime}");
            }
        }

        lines.Add("Upcoming releases:");
        if (upcomingReleases.Count == 0)
        {
            lines.Add("- No upcoming releases found.");
        }
        else
        {
            foreach (var release in upcomingReleases)
            {
                lines.Add($"- {release.Name} | Status: {release.Status} | Environment: {release.EnvironmentName}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> ReleaseNotesAsync(int daysBack, string? iterationPath)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(daysBack));
        var wiql = new StringBuilder()
            .Append("SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '")
            .Append(EscapeWiqlValue(Project))
            .Append("' AND [System.WorkItemType] IN ('User Story', 'Product Backlog Item', 'Bug') AND [System.State] IN ('Done', 'Closed', 'Resolved') AND [System.ChangedDate] >= '")
            .Append(cutoff.ToString("O"))
            .Append("'");

        if (!string.IsNullOrWhiteSpace(iterationPath))
        {
            wiql.Append(" AND [System.IterationPath] UNDER '")
                .Append(EscapeWiqlValue(iterationPath.Trim()))
                .Append("'");
        }

        wiql.Append(" ORDER BY [System.ChangedDate] DESC");

        var workItemIds = await QueryWorkItemIdsAsync(wiql.ToString());
        if (workItemIds.Count == 0)
        {
            return "No completed stories or bugs were found for the requested release notes window.";
        }

        var workItems = await GetWorkItemsAsync(workItemIds.Take(50).ToArray(),
            "System.Id",
            "System.Title",
            "System.State",
            "System.WorkItemType");

        var featureLines = workItems
            .Where(item => item.WorkItemType.Equals("User Story", StringComparison.OrdinalIgnoreCase)
                || item.WorkItemType.Equals("Product Backlog Item", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"- #{item.Id} {item.Title}")
            .ToList();

        var bugLines = workItems
            .Where(item => item.WorkItemType.Equals("Bug", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"- #{item.Id} {item.Title}")
            .ToList();

        var lines = new List<string>
        {
            $"Release notes for the last {Math.Abs(daysBack)} day(s)",
            string.Empty,
            "Features"
        };

        lines.AddRange(featureLines.Count > 0 ? featureLines : new[] { "- No completed features found." });
        lines.Add(string.Empty);
        lines.Add("Fixes");
        lines.AddRange(bugLines.Count > 0 ? bugLines : new[] { "- No completed bug fixes found." });

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> DeploymentHealthAsync(int daysBack, string? pipelineName)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(daysBack)).ToString("O");
        var endpoint = $"{Project}/_apis/build/builds?minTime={Uri.EscapeDataString(cutoff)}&queryOrder=finishTimeDescending&$top=20&api-version=7.1";
        using var buildsDocument = await GetJsonAsync(endpoint);
        var builds = ExtractBuilds(buildsDocument.RootElement, pipelineName);

        if (builds.Count == 0)
        {
            return "No recent pipeline runs were found for the requested deployment health window.";
        }

        var latest = builds[0];
        var succeeded = builds.Count(build => string.Equals(build.Result, "succeeded", StringComparison.OrdinalIgnoreCase));
        var failed = builds.Count(build => string.Equals(build.Result, "failed", StringComparison.OrdinalIgnoreCase));

        var lines = new List<string>
        {
            $"Deployment health for the last {Math.Abs(daysBack)} day(s)",
            $"Latest pipeline: {latest.DefinitionName}",
            $"Latest result: {latest.Result} ({latest.Status})",
            $"Latest finished: {latest.FinishTime}",
            $"Successful runs: {succeeded}",
            $"Failed runs: {failed}"
        };

        var appInsightsSummary = await GetApplicationInsightsSummaryAsync(daysBack);
        if (!string.IsNullOrWhiteSpace(appInsightsSummary))
        {
            lines.Add(appInsightsSummary);
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<string> BugAnalyzerAsync(string? moduleKeyword, int daysBack)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(daysBack)).ToString("O");
        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '{EscapeWiqlValue(Project)}' AND [System.WorkItemType] = 'Bug' AND [System.ChangedDate] >= '{cutoff}' ORDER BY [System.ChangedDate] DESC";
        var workItemIds = await QueryWorkItemIdsAsync(wiql);
        if (workItemIds.Count == 0)
        {
            return "No bugs were found for the requested analysis window.";
        }

        var workItems = await GetWorkItemsAsync(workItemIds.Take(100).ToArray(),
            "System.Id",
            "System.Title",
            "System.State",
            "System.AreaPath",
            "System.Tags");

        var filteredItems = workItems
            .Where(item => string.IsNullOrWhiteSpace(moduleKeyword)
                || ContainsIgnoreCase(item.Title, moduleKeyword)
                || ContainsIgnoreCase(item.AreaPath, moduleKeyword)
                || ContainsIgnoreCase(item.Tags, moduleKeyword))
            .ToList();

        if (filteredItems.Count == 0)
        {
            return $"No bugs matched the module keyword '{moduleKeyword}'.";
        }

        var recurring = filteredItems
            .GroupBy(item => item.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Take(5)
            .Select(group => $"- {group.Key} ({group.Count()} occurrence(s))")
            .ToList();

        var recent = filteredItems
            .Take(10)
            .Select(item => $"- #{item.Id} {item.Title} | State: {item.State} | Area: {item.AreaPath}")
            .ToList();

        return string.Join(Environment.NewLine,
            $"Bug analysis for the last {Math.Abs(daysBack)} day(s)",
            string.IsNullOrWhiteSpace(moduleKeyword) ? "Module filter: all bugs" : $"Module filter: {moduleKeyword}",
            $"Matching bugs: {filteredItems.Count}",
            "Recurring patterns:",
            recurring.Count > 0 ? string.Join(Environment.NewLine, recurring) : "- No recurring patterns found.",
            "Recent bugs:",
            recent.Count > 0 ? string.Join(Environment.NewLine, recent) : "- No recent bug details found.");
    }

    private async Task<string?> GetCurrentIterationPathAsync(string team)
    {
        var endpoint = $"{Project}/{Uri.EscapeDataString(team)}/_apis/work/teamsettings/iterations?$timeframe=current&api-version=7.1-preview.1";
        using var document = await GetJsonAsync(endpoint);
        if (!document.RootElement.TryGetProperty("value", out var valueElement) || valueElement.GetArrayLength() == 0)
        {
            return null;
        }

        var firstIteration = valueElement[0];
        return TryGetString(firstIteration, "path");
    }

    private async Task<string?> GetWorkItemTitleAsync(int workItemId)
    {
        var endpoint = $"_apis/wit/workitems/{workItemId}?fields=System.Title&api-version=7.1";
        using var document = await GetJsonAsync(endpoint);
        return TryGetNestedFieldString(document.RootElement, "System.Title");
    }

    private async Task<int> CreateChildTaskAsync(int parentStoryId, string taskTitle, WorkItemFieldOptions fieldOptions)
    {
        return await CreateWorkItemAsync("Task", taskTitle, null, parentStoryId, fieldOptions);
    }

    private async Task<(int WorkItemId, string WorkItemType)> CreateParentWorkItemWithFallbackAsync(
        string title,
        string? description,
        string? preferredParentWorkItemType,
        WorkItemFieldOptions fieldOptions)
    {
        Exception? lastException = null;

        foreach (var workItemType in ResolvePreferredParentTypes(preferredParentWorkItemType))
        {
            try
            {
                var createdId = await CreateWorkItemAsync(workItemType, title, description, null, fieldOptions);
                return (createdId, workItemType);
            }
            catch (Exception exception)
            {
                lastException = exception;
            }
        }

        throw new InvalidOperationException(
            $"Unable to create a parent work item for '{title}'. Tried these work item types: {string.Join(", ", ResolvePreferredParentTypes(preferredParentWorkItemType))}.",
            lastException);
    }

    private async Task<int> CreateWorkItemAsync(string workItemType, string title, string? description, int? parentWorkItemId, WorkItemFieldOptions fieldOptions)
    {
        var endpoint = $"{Project}/_apis/wit/workitems/${Uri.EscapeDataString(workItemType)}?api-version=7.1";
        var operations = new List<object>
        {
            new { op = "add", path = "/fields/System.Title", value = title }
        };

        if (!string.IsNullOrWhiteSpace(description))
        {
            operations.Add(new { op = "add", path = "/fields/System.Description", value = description.Trim() });
        }

        if (!string.IsNullOrWhiteSpace(fieldOptions.AreaPath))
        {
            operations.Add(new { op = "add", path = "/fields/System.AreaPath", value = fieldOptions.AreaPath });
        }

        if (!string.IsNullOrWhiteSpace(fieldOptions.IterationPath))
        {
            operations.Add(new { op = "add", path = "/fields/System.IterationPath", value = fieldOptions.IterationPath });
        }

        if (!string.IsNullOrWhiteSpace(fieldOptions.AssignedTo))
        {
            operations.Add(new { op = "add", path = "/fields/System.AssignedTo", value = fieldOptions.AssignedTo });
        }

        if (!string.IsNullOrWhiteSpace(fieldOptions.Tags))
        {
            operations.Add(new { op = "add", path = "/fields/System.Tags", value = fieldOptions.Tags });
        }

        if (parentWorkItemId.HasValue)
        {
            operations.Add(new
            {
                op = "add",
                path = "/relations/-",
                value = new
                {
                    rel = "System.LinkTypes.Hierarchy-Reverse",
                    url = $"{OrganizationUrl}/_apis/wit/workItems/{parentWorkItemId.Value}"
                }
            });
        }

        using var document = await PatchJsonAsync(endpoint, operations.ToArray());
        return document.RootElement.GetProperty("id").GetInt32();
    }

    private async Task<string> GetPullRequestTraceabilityAsync(string repositoryName, int pullRequestId)
    {
        var pullRequestEndpoint = $"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullRequests/{pullRequestId}?api-version=7.1";
        using var pullRequestDocument = await GetJsonAsync(pullRequestEndpoint);

        var title = TryGetString(pullRequestDocument.RootElement, "title") ?? "Untitled PR";
        var status = TryGetString(pullRequestDocument.RootElement, "status") ?? "unknown";
        var sourceBranch = TryGetString(pullRequestDocument.RootElement, "sourceRefName") ?? "unknown";
        var targetBranch = TryGetString(pullRequestDocument.RootElement, "targetRefName") ?? "unknown";

        var workItemsEndpoint = $"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullRequests/{pullRequestId}/workitems?api-version=7.1";
        using var workItemsDocument = await GetJsonAsync(workItemsEndpoint);

        var linkedIds = new List<int>();
        if (workItemsDocument.RootElement.TryGetProperty("value", out var valueElement))
        {
            foreach (var itemElement in valueElement.EnumerateArray())
            {
                if (itemElement.TryGetProperty("id", out var idElement))
                {
                    var idText = idElement.ToString();
                    if (int.TryParse(idText, out var id))
                    {
                        linkedIds.Add(id);
                    }
                }
            }
        }

        var lines = new List<string>
        {
            $"Traceability for PR #{pullRequestId} in {repositoryName}",
            $"Title: {title}",
            $"Status: {status}",
            $"Source: {sourceBranch}",
            $"Target: {targetBranch}"
        };

        if (linkedIds.Count == 0)
        {
            lines.Add("No linked Azure Boards work items were found for this pull request.");
            return string.Join(Environment.NewLine, lines);
        }

        var linkedWorkItems = await GetWorkItemsAsync(linkedIds,
            "System.Id",
            "System.Title",
            "System.State",
            "System.WorkItemType");

        lines.Add("Linked work items:");
        foreach (var workItem in linkedWorkItems)
        {
            lines.Add($"- #{workItem.Id} [{workItem.WorkItemType}] {workItem.Title} | State: {workItem.State}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private async Task<List<WorkItemSummary>> GetAssignedWorkItemsAsync(string assignedTo, bool includeClosed, int top)
    {
        var wiql = new StringBuilder()
            .Append("SELECT [System.Id] FROM WorkItems WHERE [System.TeamProject] = '")
            .Append(EscapeWiqlValue(Project))
            .Append("' AND [System.AssignedTo] = ")
            .Append(FormatWiqlIdentity(assignedTo));

        if (!includeClosed)
        {
            wiql.Append(" AND [System.State] NOT IN ('Closed', 'Done', 'Removed')");
        }

        wiql.Append(" ORDER BY [System.ChangedDate] DESC");

        var workItemIds = await QueryWorkItemIdsAsync(wiql.ToString());
        if (workItemIds.Count == 0)
        {
            return new List<WorkItemSummary>();
        }

        return await GetWorkItemsAsync(workItemIds.Take(Math.Max(1, top)).ToArray(),
            "System.Id",
            "System.Title",
            "System.State",
            "System.WorkItemType",
            "System.IterationPath",
            "System.AreaPath",
            "System.AssignedTo",
            "System.Tags",
            "System.ChangedDate");
    }

    private async Task<List<PullRequestReviewSummary>> GetPendingPrReviewsAsync(string? repositoryName, string reviewerIdentity, int top)
    {
        var resolvedRepository = string.IsNullOrWhiteSpace(repositoryName) ? DefaultRepository : repositoryName.Trim();
        if (string.IsNullOrWhiteSpace(resolvedRepository))
        {
            return new List<PullRequestReviewSummary>();
        }

        var endpoint = $"{Project}/_apis/git/repositories/{Uri.EscapeDataString(resolvedRepository)}/pullrequests?searchCriteria.status=active&$top={Math.Max(1, top * 3)}&api-version=7.1";
        using var document = await GetJsonAsync(endpoint);
        if (!document.RootElement.TryGetProperty("value", out var valueElement))
        {
            return new List<PullRequestReviewSummary>();
        }

        var results = new List<PullRequestReviewSummary>();
        foreach (var prElement in valueElement.EnumerateArray())
        {
            if (!prElement.TryGetProperty("reviewers", out var reviewersElement))
            {
                continue;
            }

            var hasPendingReview = reviewersElement.EnumerateArray().Any(reviewer =>
            {
                var displayName = TryGetString(reviewer, "displayName") ?? string.Empty;
                var uniqueName = TryGetString(reviewer, "uniqueName") ?? string.Empty;
                var vote = reviewer.TryGetProperty("vote", out var voteElement) ? voteElement.GetInt32() : 0;
                return vote == 0 &&
                    (string.Equals(displayName, reviewerIdentity, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(uniqueName, reviewerIdentity, StringComparison.OrdinalIgnoreCase)
                        || ContainsIgnoreCase(displayName, reviewerIdentity)
                        || ContainsIgnoreCase(uniqueName, reviewerIdentity));
            });

            if (!hasPendingReview)
            {
                continue;
            }

            results.Add(new PullRequestReviewSummary(
                prElement.GetProperty("pullRequestId").GetInt32(),
                TryGetString(prElement, "title") ?? "Untitled PR",
                TryGetString(prElement, "sourceRefName") ?? "unknown",
                TryGetString(prElement, "targetRefName") ?? "unknown"));
        }

        return results.Take(top).ToList();
    }

    private async Task<List<BuildSummary>> GetFailedBuildsAsync(int daysBack, int top)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Abs(daysBack)).ToString("O");
        var endpoint = $"{Project}/_apis/build/builds?minTime={Uri.EscapeDataString(cutoff)}&queryOrder=finishTimeDescending&$top={Math.Max(1, top * 3)}&api-version=7.1";
        using var buildsDocument = await GetJsonAsync(endpoint);
        return ExtractBuilds(buildsDocument.RootElement, null)
            .Where(build => string.Equals(build.Result, "failed", StringComparison.OrdinalIgnoreCase))
            .Take(top)
            .ToList();
    }

    private async Task<List<ReleaseSummary>> GetUpcomingReleasesAsync(int daysBack, int top)
    {
        try
        {
            using var client = CreateReleaseClient();
            var endpoint = $"{Project}/_apis/release/releases?$top={Math.Max(1, top)}&queryOrder=descending&statusFilter=active&api-version=7.1";
            using var response = await client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                return new List<ReleaseSummary>();
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var document = await JsonDocument.ParseAsync(stream);
            if (!document.RootElement.TryGetProperty("value", out var valueElement))
            {
                return new List<ReleaseSummary>();
            }

            var results = new List<ReleaseSummary>();
            foreach (var releaseElement in valueElement.EnumerateArray())
            {
                var name = TryGetString(releaseElement, "name") ?? "Unnamed release";
                var status = TryGetString(releaseElement, "status") ?? "unknown";
                var environmentName = "n/a";

                if (releaseElement.TryGetProperty("environments", out var environmentsElement) && environmentsElement.GetArrayLength() > 0)
                {
                    environmentName = TryGetString(environmentsElement[0], "name") ?? "n/a";
                }

                results.Add(new ReleaseSummary(name, status, environmentName));
            }

            return results.Take(top).ToList();
        }
        catch
        {
            return new List<ReleaseSummary>();
        }
    }

    private async Task<string> GetWorkItemTraceabilityAsync(int workItemId)
    {
        var endpoint = $"_apis/wit/workitems/{workItemId}?$expand=relations&api-version=7.1";
        using var document = await GetJsonAsync(endpoint);

        var title = TryGetNestedFieldString(document.RootElement, "System.Title") ?? "Untitled";
        var state = TryGetNestedFieldString(document.RootElement, "System.State") ?? "Unknown";
        var workItemType = TryGetNestedFieldString(document.RootElement, "System.WorkItemType") ?? "Unknown";

        var relationLines = new List<string>();
        if (document.RootElement.TryGetProperty("relations", out var relationsElement))
        {
            foreach (var relationElement in relationsElement.EnumerateArray())
            {
                var rel = TryGetString(relationElement, "rel") ?? "unknown";
                var url = TryGetString(relationElement, "url") ?? "unknown";

                if (ContainsIgnoreCase(rel, "ArtifactLink") || ContainsIgnoreCase(url, "pullrequest") || ContainsIgnoreCase(url, "commit"))
                {
                    relationLines.Add($"- {rel}: {url}");
                }
            }
        }

        if (relationLines.Count == 0)
        {
            relationLines.Add("- No linked code artifacts were found on this work item.");
        }

        return string.Join(Environment.NewLine,
            $"Traceability for work item #{workItemId}",
            $"Type: {workItemType}",
            $"Title: {title}",
            $"State: {state}",
            "Linked code artifacts:",
            string.Join(Environment.NewLine, relationLines));
    }

    private async Task<List<int>> QueryWorkItemIdsAsync(string wiql)
    {
        using var document = await PostJsonAsync("_apis/wit/wiql?api-version=7.1", new { query = wiql });

        if (!document.RootElement.TryGetProperty("workItems", out var workItemsElement))
        {
            return new List<int>();
        }

        var workItemIds = new List<int>();
        foreach (var workItemElement in workItemsElement.EnumerateArray())
        {
            if (workItemElement.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id))
            {
                workItemIds.Add(id);
            }
        }

        return workItemIds;
    }

    private async Task<List<WorkItemSummary>> GetWorkItemsAsync(IEnumerable<int> ids, params string[] fields)
    {
        var request = new
        {
            ids = ids.ToArray(),
            fields,
            errorPolicy = "Omit"
        };

        using var document = await PostJsonAsync("_apis/wit/workitemsbatch?api-version=7.1", request);
        if (!document.RootElement.TryGetProperty("value", out var valueElement))
        {
            return new List<WorkItemSummary>();
        }

        var workItems = new List<WorkItemSummary>();
        foreach (var workItemElement in valueElement.EnumerateArray())
        {
            var summary = new WorkItemSummary(
                workItemElement.GetProperty("id").GetInt32(),
                TryGetNestedFieldString(workItemElement, "System.Title") ?? "Untitled",
                TryGetNestedFieldString(workItemElement, "System.State") ?? "Unknown",
                TryGetNestedFieldString(workItemElement, "System.WorkItemType") ?? "Unknown",
                TryGetNestedFieldString(workItemElement, "System.IterationPath") ?? "n/a",
                TryGetNestedFieldString(workItemElement, "System.AreaPath") ?? "n/a",
                TryGetNestedFieldString(workItemElement, "System.AssignedTo") ?? "n/a",
                TryGetNestedFieldString(workItemElement, "System.Tags") ?? string.Empty,
                TryGetNestedFieldDateTime(workItemElement, "System.ChangedDate"));

            workItems.Add(summary);
        }

        return workItems;
    }

    private async Task<string> GetApplicationInsightsSummaryAsync(int daysBack)
    {
        if (string.IsNullOrWhiteSpace(ApplicationInsightsAppId) || string.IsNullOrWhiteSpace(ApplicationInsightsApiKey))
        {
            return "Application Insights summary: not configured.";
        }

        var query = $"requests | where timestamp > ago({Math.Abs(daysBack)}d) | summarize total=count(), failed=countif(success == false)";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-api-key", ApplicationInsightsApiKey);
        var url = $"https://api.applicationinsights.io/v1/apps/{ApplicationInsightsAppId}/query?query={Uri.EscapeDataString(query)}";
        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return $"Application Insights summary: query failed with HTTP {(int)response.StatusCode}.";
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        if (!document.RootElement.TryGetProperty("tables", out var tablesElement) || tablesElement.GetArrayLength() == 0)
        {
            return "Application Insights summary: no telemetry returned.";
        }

        var rows = tablesElement[0].GetProperty("rows");
        if (rows.GetArrayLength() == 0)
        {
            return "Application Insights summary: no telemetry returned.";
        }

        var firstRow = rows[0];
        var total = firstRow[0].GetInt32();
        var failed = firstRow[1].GetInt32();
        return $"Application Insights summary: {failed} failed request(s) out of {total} total request(s).";
    }

    private async Task<JsonDocument> GetJsonAsync(string relativePath)
    {
        using var client = CreateAzureDevOpsClient();
        using var response = await client.GetAsync(relativePath);
        return await ReadJsonResponseAsync(response);
    }

    private async Task<JsonDocument> PostJsonAsync(string relativePath, object payload)
    {
        using var client = CreateAzureDevOpsClient();
        using var content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(relativePath, content);
        return await ReadJsonResponseAsync(response);
    }

    private async Task<JsonDocument> PatchJsonAsync(string relativePath, object payload)
    {
        using var client = CreateAzureDevOpsClient();
        using var request = new HttpRequestMessage(HttpMethod.Patch, relativePath)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json-patch+json")
        };

        using var response = await client.SendAsync(request);
        return await ReadJsonResponseAsync(response);
    }

    private HttpClient CreateReleaseClient()
    {
        var releaseBaseAddress = BuildReleaseBaseAddress();
        var client = new HttpClient
        {
            BaseAddress = new Uri(releaseBaseAddress)
        };

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{PersonalAccessToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        return client;
    }

    private HttpClient CreateAzureDevOpsClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri($"{OrganizationUrl}/")
        };

        var token = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{PersonalAccessToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        return client;
    }

    private string BuildReleaseBaseAddress()
    {
        var organizationUri = new Uri($"{OrganizationUrl}/");
        var releaseHost = organizationUri.Host.StartsWith("vsrm.", StringComparison.OrdinalIgnoreCase)
            ? organizationUri.Host
            : $"vsrm.{organizationUri.Host}";

        return $"{organizationUri.Scheme}://{releaseHost}{organizationUri.AbsolutePath}";
    }

    private static async Task<JsonDocument> ReadJsonResponseAsync(HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Azure DevOps request failed with HTTP {(int)response.StatusCode}: {responseBody}");
        }

        return JsonDocument.Parse(responseBody);
    }

    private static List<string> GenerateTaskBreakdown(string storyTitle, string? storyDescription, int taskCount)
    {
        var effectiveTaskCount = Math.Clamp(taskCount, 1, 10);
        var suggestedTasks = new List<string>
        {
            $"Review acceptance criteria and dependencies for {storyTitle}",
            $"Implement backend or service changes for {storyTitle}",
            $"Implement UI or integration flow for {storyTitle}",
            $"Add automated test coverage for {storyTitle}",
            $"Validate end-to-end behavior and update documentation for {storyTitle}"
        };

        if (!string.IsNullOrWhiteSpace(storyDescription) && ContainsIgnoreCase(storyDescription, "api"))
        {
            suggestedTasks.Insert(2, $"Update API contracts and payload validation for {storyTitle}");
        }

        while (suggestedTasks.Count < effectiveTaskCount)
        {
            suggestedTasks.Add($"Additional implementation task {suggestedTasks.Count + 1} for {storyTitle}");
        }

        return suggestedTasks.Take(effectiveTaskCount).ToList();
    }

    private WorkItemFieldOptions ResolveWorkItemFieldOptions(
        string? tags,
        string? areaPath,
        string? iterationPath,
        string? assignee,
        string featureType)
    {
        var mergedTags = MergeTags(DefaultTags, tags, "AI-Generated", $"Template:{featureType}");

        return new WorkItemFieldOptions(
            string.IsNullOrWhiteSpace(areaPath) ? DefaultAreaPath : areaPath.Trim(),
            string.IsNullOrWhiteSpace(iterationPath) ? DefaultIterationPath : iterationPath.Trim(),
            string.IsNullOrWhiteSpace(assignee)
                ? (string.IsNullOrWhiteSpace(DefaultAssignee) ? AssignedTo : DefaultAssignee)
                : assignee.Trim(),
            mergedTags);
    }

    private static string ResolveFeatureType(string? featureType)
    {
        return string.IsNullOrWhiteSpace(featureType)
            ? "fullstack"
            : featureType.Trim().ToLowerInvariant();
    }

    private static List<string> GenerateSmartFeatureTasks(string featureTitle, string uiTechnology, string featureType)
    {
        return featureType switch
        {
            "api" => new List<string>
            {
                $"API design and contract task for {featureTitle}",
                $"Backend implementation task for {featureTitle}",
                $"Integration testing task for {featureTitle}",
                $"API documentation task for {featureTitle}",
                $"Deployment validation task for {featureTitle}"
            },
            "ui" => new List<string>
            {
                $"UX and functional design task for {featureTitle}",
                $"{uiTechnology} UI implementation task for {featureTitle}",
                $"API integration task for {featureTitle}",
                $"UI unit and E2E testing task for {featureTitle}",
                $"Deployment verification task for {featureTitle}"
            },
            "integration" => new List<string>
            {
                $"Integration requirements review task for {featureTitle}",
                $"Service connector implementation task for {featureTitle}",
                $"Error handling and retry logic task for {featureTitle}",
                $"Integration testing task for {featureTitle}",
                $"Deployment and monitoring task for {featureTitle}"
            },
            "data" => new List<string>
            {
                $"Schema or migration task for {featureTitle}",
                $"Data access implementation task for {featureTitle}",
                $"API update task for {featureTitle}",
                $"Data validation and unit testing task for {featureTitle}",
                $"Deployment and rollback task for {featureTitle}"
            },
            "security" => new List<string>
            {
                $"Security requirements and threat review task for {featureTitle}",
                $"Backend security implementation task for {featureTitle}",
                $"{uiTechnology} security UX task for {featureTitle}",
                $"Security test coverage task for {featureTitle}",
                $"Deployment hardening and monitoring task for {featureTitle}"
            },
            "bugfix" => new List<string>
            {
                $"Bug reproduction and root cause analysis task for {featureTitle}",
                $"Code fix implementation task for {featureTitle}",
                $"Regression and unit testing task for {featureTitle}",
                $"Release validation task for {featureTitle}",
                $"Deployment task for {featureTitle}"
            },
            _ => new List<string>
            {
                $"Backend task for {featureTitle}",
                $"{uiTechnology} UI task for {featureTitle}",
                $"API task for {featureTitle}",
                $"Unit testing task for {featureTitle}",
                $"Deployment task for {featureTitle}"
            }
        };
    }

    private static IReadOnlyList<string> ResolvePreferredParentTypes(string? preferredParentWorkItemType)
    {
        var parentTypes = new List<string>();

        if (!string.IsNullOrWhiteSpace(preferredParentWorkItemType))
        {
            parentTypes.Add(preferredParentWorkItemType.Trim());
        }

        foreach (var candidate in new[] { "User Story", "Product Backlog Item", "Feature", "Issue" })
        {
            if (!parentTypes.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                parentTypes.Add(candidate);
            }
        }

        return parentTypes;
    }

    private static List<BuildSummary> ExtractBuilds(JsonElement rootElement, string? pipelineName)
    {
        if (!rootElement.TryGetProperty("value", out var valueElement))
        {
            return new List<BuildSummary>();
        }

        var builds = new List<BuildSummary>();
        foreach (var buildElement in valueElement.EnumerateArray())
        {
            var definitionName = buildElement.TryGetProperty("definition", out var definitionElement)
                ? TryGetString(definitionElement, "name") ?? "Unknown"
                : "Unknown";

            if (!string.IsNullOrWhiteSpace(pipelineName) && !ContainsIgnoreCase(definitionName, pipelineName))
            {
                continue;
            }

            builds.Add(new BuildSummary(
                definitionName,
                TryGetString(buildElement, "status") ?? "Unknown",
                TryGetString(buildElement, "result") ?? "Unknown",
                TryGetString(buildElement, "finishTime") ?? "n/a"));
        }

        return builds;
    }

    private static string? TryGetNestedFieldString(JsonElement element, string fieldName)
    {
        if (!element.TryGetProperty("fields", out var fieldsElement) || !fieldsElement.TryGetProperty(fieldName, out var fieldElement))
        {
            return null;
        }

        return fieldElement.ValueKind switch
        {
            JsonValueKind.String => fieldElement.GetString(),
            JsonValueKind.Object when fieldElement.TryGetProperty("displayName", out var displayNameElement) => displayNameElement.GetString(),
            JsonValueKind.Object when fieldElement.TryGetProperty("name", out var nameElement) => nameElement.GetString(),
            _ => fieldElement.ToString()
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var propertyElement) && propertyElement.ValueKind == JsonValueKind.String
            ? propertyElement.GetString()
            : propertyElement.ToString();
    }

    private static DateTime? TryGetNestedFieldDateTime(JsonElement element, string fieldName)
    {
        var rawValue = TryGetNestedFieldString(element, fieldName);
        return DateTime.TryParse(rawValue, out var parsedDate) ? parsedDate : null;
    }

    private static string EscapeWiqlValue(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static string FormatWiqlIdentity(string value)
    {
        return string.Equals(value, "@Me", StringComparison.OrdinalIgnoreCase)
            ? "@Me"
            : $"'{EscapeWiqlValue(value)}'";
    }

    private static bool ContainsIgnoreCase(string? source, string value)
        => !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool IsBlocked(WorkItemSummary item)
    {
        return ContainsIgnoreCase(item.State, "block")
            || ContainsIgnoreCase(item.Tags, "blocked")
            || ContainsIgnoreCase(item.Tags, "impediment");
    }

    private static string? MergeTags(params string?[] tagSources)
    {
        var tags = new List<string>();

        foreach (var tagSource in tagSources)
        {
            if (string.IsNullOrWhiteSpace(tagSource))
            {
                continue;
            }

            foreach (var tag in tagSource.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    tags.Add(tag);
                }
            }
        }

        return tags.Count == 0 ? null : string.Join("; ", tags);
    }

    private static string CalculateSprintRisk(double completionPercent, int delayedTaskCount, int overloadedDeveloperCount, double averageActiveItems)
    {
        var riskScore = 0;

        if (completionPercent < 40)
        {
            riskScore += 2;
        }
        else if (completionPercent < 70)
        {
            riskScore += 1;
        }

        if (delayedTaskCount >= 5)
        {
            riskScore += 2;
        }
        else if (delayedTaskCount >= 2)
        {
            riskScore += 1;
        }

        if (overloadedDeveloperCount >= 3 || averageActiveItems >= 6)
        {
            riskScore += 2;
        }
        else if (overloadedDeveloperCount >= 1 || averageActiveItems >= 4)
        {
            riskScore += 1;
        }

        return riskScore switch
        {
            >= 5 => "High",
            >= 3 => "Medium",
            _ => "Low"
        };
    }

    private static string FormatDate(DateTime? value)
        => value.HasValue ? value.Value.ToString("u") : "n/a";

    private sealed record WorkItemSummary(
        int Id,
        string Title,
        string State,
        string WorkItemType,
        string IterationPath,
        string AreaPath,
        string AssignedTo,
        string Tags,
        DateTime? ChangedDateUtc);

    private sealed record BuildSummary(
        string DefinitionName,
        string Status,
        string Result,
        string FinishTime);

    private sealed record PullRequestReviewSummary(
        int PullRequestId,
        string Title,
        string SourceBranch,
        string TargetBranch);

    private sealed record ReleaseSummary(
        string Name,
        string Status,
        string EnvironmentName);

    private sealed record WorkItemFieldOptions(
        string? AreaPath,
        string? IterationPath,
        string? AssignedTo,
        string? Tags);

    private sealed record DeveloperWorkload(
        string AssignedTo,
        int ActiveItems);
}