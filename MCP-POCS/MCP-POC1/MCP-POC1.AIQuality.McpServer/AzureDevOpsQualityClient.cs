using MCP_POC1.Shared;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MCP_POC1.AIQuality.McpServer;

internal sealed class AzureDevOpsQualityClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static bool TryCreate(out AzureDevOpsQualityClient? client, out string configurationMessage)
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
            configurationMessage = "Azure DevOps AI quality tools are not configured yet. Set these environment variables: "
                + string.Join(", ", missingSettings)
                + ". Optional settings: AZDO_REPOSITORY, AZDO_DEFAULT_AREA_PATH, AZDO_DEFAULT_ITERATION_PATH, AZDO_DEFAULT_ASSIGNEE, AZDO_DEFAULT_TAGS.";
            return false;
        }

        client = new AzureDevOpsQualityClient(
            organizationUrl!.TrimEnd('/'),
            project!,
            personalAccessToken!,
            EnvironmentSettings.ReadFirst("AZDO_REPOSITORY"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_AREA_PATH"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_ITERATION_PATH"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_ASSIGNEE"),
            EnvironmentSettings.ReadFirst("AZDO_DEFAULT_TAGS"));
        configurationMessage = string.Empty;
        return true;
    }

    private AzureDevOpsQualityClient(
        string organizationUrl,
        string project,
        string personalAccessToken,
        string? defaultRepository,
        string? defaultAreaPath,
        string? defaultIterationPath,
        string? defaultAssignee,
        string? defaultTags)
    {
        OrganizationUrl = organizationUrl;
        Project = project;
        PersonalAccessToken = personalAccessToken;
        DefaultRepository = defaultRepository;
        DefaultAreaPath = defaultAreaPath;
        DefaultIterationPath = defaultIterationPath;
        DefaultAssignee = defaultAssignee;
        DefaultTags = defaultTags;
    }

    public string? DefaultRepository { get; }

    private string OrganizationUrl { get; }

    private string Project { get; }

    private string PersonalAccessToken { get; }

    private string? DefaultAreaPath { get; }

    private string? DefaultIterationPath { get; }

    private string? DefaultAssignee { get; }

    private string? DefaultTags { get; }

    public async Task<string> GetRepositoryIntelligenceAsync(string? repositoryName, int? pullRequestId)
    {
        var resolvedRepository = ResolveRepositoryName(repositoryName);
        if (string.IsNullOrWhiteSpace(resolvedRepository))
        {
            return "A repositoryName is required unless AZDO_REPOSITORY is configured.";
        }

        using var repositoryDocument = await GetJsonAsync($"{Project}/_apis/git/repositories/{Uri.EscapeDataString(resolvedRepository)}?api-version=7.1");
        var defaultBranch = TryGetString(repositoryDocument.RootElement, "defaultBranch") ?? "unknown";
        var remoteUrl = TryGetString(repositoryDocument.RootElement, "remoteUrl") ?? "n/a";
        var activePrCount = await GetActivePullRequestCountAsync(resolvedRepository);

        var lines = new List<string>
        {
            $"Repository intelligence for {resolvedRepository}",
            $"Default branch: {defaultBranch}",
            $"Remote URL: {remoteUrl}",
            $"Active pull requests: {activePrCount}"
        };

        if (!pullRequestId.HasValue)
        {
            return string.Join(Environment.NewLine, lines);
        }

        var pullRequest = await GetPullRequestContextAsync(resolvedRepository, pullRequestId.Value);
        lines.Add($"Pull request: #{pullRequest.PullRequestId} {pullRequest.Title}");
        lines.Add($"Status: {pullRequest.Status}");
        lines.Add($"Source -> Target: {pullRequest.SourceBranch} -> {pullRequest.TargetBranch}");
        lines.Add($"Changed files: {pullRequest.ChangedFiles.Count}");
        lines.Add($"Linked work items: {pullRequest.LinkedWorkItemIds.Count}");

        foreach (var file in pullRequest.ChangedFiles.Take(10))
        {
            lines.Add($"- {file}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<PullRequestContext> GetPullRequestContextAsync(string repositoryName, int pullRequestId)
    {
        using var pullRequestDocument = await GetJsonAsync($"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullRequests/{pullRequestId}?api-version=7.1");

        var changedFiles = await GetChangedFilesAsync(repositoryName, pullRequestId);
        var linkedWorkItemIds = await GetLinkedWorkItemIdsAsync(repositoryName, pullRequestId);

        return new PullRequestContext(
            pullRequestId,
            TryGetString(pullRequestDocument.RootElement, "title") ?? "Untitled PR",
            TryGetString(pullRequestDocument.RootElement, "status") ?? "unknown",
            TryGetString(pullRequestDocument.RootElement, "sourceRefName") ?? "unknown",
            TryGetString(pullRequestDocument.RootElement, "targetRefName") ?? "unknown",
            changedFiles,
            linkedWorkItemIds);
    }

    public async Task<string> CreateRemediationWorkItemsAsync(RemediationPlan plan, bool createInAzureDevOps, string? assignee, string? tags)
    {
        var lines = new List<string>
        {
            $"Remediation plan for {plan.Title}",
            $"Priority: {plan.Priority}",
            $"Effort: {plan.Effort}",
            $"Summary: {plan.Summary}",
            $"Epic: {plan.EpicTitle}",
            $"Feature: {plan.FeatureTitle}",
            $"Story: {plan.StoryTitle}",
            "Acceptance criteria:",
            plan.AcceptanceCriteria,
            "Validation plan:",
            plan.ValidationPlan,
            "Risk notes:",
            plan.RiskNotes.Count > 0 ? string.Join(Environment.NewLine, plan.RiskNotes.Select(note => $"- {note}")) : "- No additional risk notes.",
            "Actions:"
        };

        foreach (var action in plan.Actions)
        {
            lines.Add($"- [{action.Category}] {action.Title}: {action.Description}");
        }

        if (!createInAzureDevOps)
        {
            lines.Add("Azure Boards creation not requested. Preview hierarchy: Epic -> Feature -> User Story/Product Backlog Item -> Tasks.");
            return string.Join(Environment.NewLine, lines);
        }

        var mergedTags = MergeTags(tags, DefaultTags, "ai-quality", $"priority-{plan.Priority.ToLowerInvariant()}", $"effort-{plan.Effort.ToLowerInvariant()}");
        var resolvedAssignee = string.IsNullOrWhiteSpace(assignee) ? DefaultAssignee : assignee.Trim();
        var fieldOptions = new WorkItemFieldOptions(DefaultAreaPath, DefaultIterationPath, resolvedAssignee, mergedTags);

        var epicDescription = BuildEpicDescription(plan);
        var featureDescription = BuildFeatureDescription(plan);
        var storyDescription = BuildStoryDescription(plan);

        var (epicId, epicType) = await CreateParentWorkItemWithFallbackAsync(plan.EpicTitle, epicDescription, new[] { "Epic", "Feature", "Issue" }, fieldOptions);
        var (featureId, featureType) = await CreateParentWorkItemWithFallbackAsync(plan.FeatureTitle, featureDescription, new[] { "Feature", "Epic", "Issue" }, fieldOptions, epicId);
        var (storyId, storyType) = await CreateParentWorkItemWithFallbackAsync(plan.StoryTitle, storyDescription, new[] { "User Story", "Product Backlog Item", "Issue", "Bug" }, fieldOptions, featureId);
        var taskIds = new List<int>();

        foreach (var action in plan.Actions)
        {
            var taskDescription = BuildTaskDescription(plan, action);
            taskIds.Add(await CreateWorkItemAsync("Task", action.Title, taskDescription, storyId, fieldOptions));
        }

        lines.Add($"Created {epicType} #{epicId}");
        lines.Add($"Created {featureType} #{featureId}");
        lines.Add($"Created {storyType} #{storyId}");
        lines.Add($"Created Tasks: {string.Join(", ", taskIds.Select(id => $"#{id}"))}");
        return string.Join(Environment.NewLine, lines);
    }

    private async Task<(int WorkItemId, string WorkItemType)> CreateParentWorkItemWithFallbackAsync(
        string title,
        string? description,
        IEnumerable<string> candidateTypes,
        WorkItemFieldOptions fieldOptions,
        int? parentWorkItemId = null)
    {
        Exception? lastException = null;

        foreach (var workItemType in candidateTypes.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var createdId = await CreateWorkItemAsync(workItemType, title, description, parentWorkItemId, fieldOptions);
                return (createdId, workItemType);
            }
            catch (Exception exception)
            {
                lastException = exception;
            }
        }

        throw new InvalidOperationException(
            $"Unable to create a parent work item for '{title}'. Tried these work item types: {string.Join(", ", candidateTypes)}.",
            lastException);
    }

    public async Task<string> PostPullRequestReviewCommentAsync(string repositoryName, int pullRequestId, string comment)
    {
        var payload = new
        {
            comments = new[]
            {
                new
                {
                    parentCommentId = 0,
                    content = comment,
                    commentType = 1
                }
            },
            status = "active"
        };

        await PostJsonAsync($"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullRequests/{pullRequestId}/threads?api-version=7.1", payload);
        return $"Posted AI quality review comment to PR #{pullRequestId} in {repositoryName}.";
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

    private async Task<int> GetActivePullRequestCountAsync(string repositoryName)
    {
        using var document = await GetJsonAsync($"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullrequests?searchCriteria.status=active&$top=200&api-version=7.1");
        return document.RootElement.TryGetProperty("count", out var countElement) && countElement.TryGetInt32(out var count)
            ? count
            : document.RootElement.TryGetProperty("value", out var valueElement) ? valueElement.GetArrayLength() : 0;
    }

    private async Task<IReadOnlyList<int>> GetLinkedWorkItemIdsAsync(string repositoryName, int pullRequestId)
    {
        using var workItemsDocument = await GetJsonAsync($"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullRequests/{pullRequestId}/workitems?api-version=7.1");
        if (!workItemsDocument.RootElement.TryGetProperty("value", out var valueElement))
        {
            return Array.Empty<int>();
        }

        var ids = new List<int>();
        foreach (var itemElement in valueElement.EnumerateArray())
        {
            var idText = TryGetString(itemElement, "id");
            if (int.TryParse(idText, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private async Task<IReadOnlyList<string>> GetChangedFilesAsync(string repositoryName, int pullRequestId)
    {
        using var iterationsDocument = await GetJsonAsync($"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullRequests/{pullRequestId}/iterations?api-version=7.1");
        if (!iterationsDocument.RootElement.TryGetProperty("value", out var iterationsElement) || iterationsElement.GetArrayLength() == 0)
        {
            return Array.Empty<string>();
        }

        var latestIterationId = iterationsElement.EnumerateArray()
            .Select(iteration => iteration.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id) ? id : 0)
            .DefaultIfEmpty(0)
            .Max();

        if (latestIterationId <= 0)
        {
            return Array.Empty<string>();
        }

        using var changesDocument = await GetJsonAsync($"{Project}/_apis/git/repositories/{Uri.EscapeDataString(repositoryName)}/pullRequests/{pullRequestId}/iterations/{latestIterationId}/changes?$top=200&api-version=7.1");
        JsonElement changesElement;
        if (!changesDocument.RootElement.TryGetProperty("changeEntries", out changesElement)
            && !changesDocument.RootElement.TryGetProperty("changes", out changesElement)
            && !changesDocument.RootElement.TryGetProperty("value", out changesElement))
        {
            return Array.Empty<string>();
        }

        var changedFiles = new List<string>();
        foreach (var entry in changesElement.EnumerateArray())
        {
            if (!entry.TryGetProperty("item", out var itemElement))
            {
                continue;
            }

            var path = TryGetString(itemElement, "path");
            if (!string.IsNullOrWhiteSpace(path))
            {
                changedFiles.Add(path);
            }
        }

        return changedFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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

    private async Task<JsonDocument> ReadJsonResponseAsync(HttpResponseMessage response)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        if (!response.IsSuccessStatusCode)
        {
            using var reader = new StreamReader(stream);
            var body = await reader.ReadToEndAsync();
            throw new InvalidOperationException($"Azure DevOps request failed with HTTP {(int)response.StatusCode}: {body}");
        }

        return await JsonDocument.ParseAsync(stream);
    }

    private HttpClient CreateAzureDevOpsClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(OrganizationUrl + "/")
        };

        var tokenBytes = Encoding.ASCII.GetBytes($":{PersonalAccessToken}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(tokenBytes));
        return client;
    }

    private string? ResolveRepositoryName(string? repositoryName)
    {
        return string.IsNullOrWhiteSpace(repositoryName) ? DefaultRepository : repositoryName.Trim();
    }

    private static string BuildEpicDescription(RemediationPlan plan)
    {
        var lines = new List<string>
        {
            plan.Summary,
            string.Empty,
            $"Priority: {plan.Priority}",
            $"Effort: {plan.Effort}",
            string.Empty,
            "Risk notes:",
            plan.RiskNotes.Count > 0 ? string.Join(Environment.NewLine, plan.RiskNotes.Select(note => $"- {note}")) : "- No additional risk notes."
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildFeatureDescription(RemediationPlan plan)
    {
        var lines = new List<string>
        {
            plan.Summary,
            string.Empty,
            "Acceptance criteria:",
            plan.AcceptanceCriteria
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildStoryDescription(RemediationPlan plan)
    {
        var lines = new List<string>
        {
            plan.Summary,
            string.Empty,
            "Acceptance criteria:",
            plan.AcceptanceCriteria,
            string.Empty,
            "Validation plan:",
            plan.ValidationPlan
        };

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildTaskDescription(RemediationPlan plan, RemediationAction action)
    {
        var lines = new List<string>
        {
            action.Description,
            string.Empty,
            $"Category: {action.Category}",
            $"Parent remediation: {plan.Title}"
        };

        if (string.Equals(action.Category, "Validation", StringComparison.OrdinalIgnoreCase))
        {
            lines.Add(string.Empty);
            lines.Add("Validation guidance:");
            lines.Add(plan.ValidationPlan);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string MergeTags(params string?[] values)
    {
        return string.Join("; ", values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .SelectMany(value => value!.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.ToString() : null;
    }

    private sealed record WorkItemFieldOptions(
        string? AreaPath,
        string? IterationPath,
        string? AssignedTo,
        string? Tags);
}