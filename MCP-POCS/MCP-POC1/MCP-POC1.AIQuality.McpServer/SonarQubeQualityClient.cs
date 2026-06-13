using MCP_POC1.Shared;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MCP_POC1.AIQuality.McpServer;

internal sealed class SonarQubeQualityClient
{
    public static bool TryCreate(out SonarQubeQualityClient? client, out string configurationMessage)
    {
        var baseUrl = EnvironmentSettings.ReadFirst("SONARQUBE_URL");
        var token = EnvironmentSettings.ReadFirst("SONARQUBE_TOKEN");

        var missingSettings = new List<string>();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            missingSettings.Add("SONARQUBE_URL");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            missingSettings.Add("SONARQUBE_TOKEN");
        }

        if (missingSettings.Count > 0)
        {
            client = null;
            configurationMessage = "SonarQube MCP tools are not configured yet. Set these environment variables: "
                + string.Join(", ", missingSettings)
                + ".";
            return false;
        }

        client = new SonarQubeQualityClient(baseUrl!, token!);
        configurationMessage = string.Empty;
        return true;
    }

    private SonarQubeQualityClient(string baseUrl, string token)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        Token = token;
    }

    private string BaseUrl { get; }

    private string Token { get; }

    public async Task<string> GetQualityOverviewAsync(string projectKey, string? branch, int top)
    {
        var issues = await GetIssuesAsync(projectKey, branch, Math.Max(1, top));
        var hotspots = await GetHotspotsAsync(projectKey, branch, Math.Max(1, Math.Min(top, 10)));
        var qualityGate = await GetQualityGateAsync(projectKey, branch);

        var lines = new List<string>
        {
            $"SonarQube quality overview for {projectKey}",
            $"Quality gate: {qualityGate.status}"
        };

        var failingConditions = qualityGate.conditions
            .Where(condition => !string.Equals(condition.Status, "OK", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();

        if (failingConditions.Count > 0)
        {
            lines.Add("Failing quality gate conditions:");
            foreach (var condition in failingConditions)
            {
                lines.Add($"- {condition.Metric}: {condition.ActualValue ?? "n/a"} vs threshold {condition.Threshold ?? "n/a"} ({condition.Status})");
            }
        }

        lines.Add($"Open issues analyzed: {issues.Count}");
        foreach (var issueType in issues.GroupBy(issue => issue.Type).OrderBy(group => group.Key))
        {
            lines.Add($"- {issueType.Key}: {issueType.Count()}");
        }

        if (issues.Count > 0)
        {
            lines.Add("Top issues:");
            foreach (var issue in issues.Take(Math.Min(top, 10)))
            {
                lines.Add($"- [{issue.Severity}] [{issue.Type}] {issue.Message} | File: {SimplifyComponent(issue.Component)} | Rule: {issue.Rule}");
            }
        }
        else
        {
            lines.Add("No open issues were returned by SonarQube.");
        }

        lines.Add($"Security hotspots: {hotspots.Count}");
        foreach (var hotspot in hotspots.Take(5))
        {
            lines.Add($"- [{hotspot.VulnerabilityProbability}] {hotspot.Message} | File: {SimplifyComponent(hotspot.Component)} | Status: {hotspot.Status}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<IReadOnlyList<SonarIssue>> GetIssuesAsync(string projectKey, string? branch, int top)
    {
        var queryParts = new List<string>
        {
            $"componentKeys={Uri.EscapeDataString(projectKey.Trim())}",
            $"ps={Math.Max(1, top)}",
            "types=BUG,VULNERABILITY,CODE_SMELL"
        };

        if (!string.IsNullOrWhiteSpace(branch))
        {
            queryParts.Add($"branch={Uri.EscapeDataString(branch.Trim())}");
        }

        using var document = await GetJsonAsync($"api/issues/search?{string.Join("&", queryParts)}");
        if (!document.RootElement.TryGetProperty("issues", out var issuesElement))
        {
            return Array.Empty<SonarIssue>();
        }

        var issues = new List<SonarIssue>();
        foreach (var issueElement in issuesElement.EnumerateArray())
        {
            issues.Add(new SonarIssue(
                TryGetString(issueElement, "key") ?? string.Empty,
                TryGetString(issueElement, "rule") ?? "unknown",
                TryGetString(issueElement, "severity") ?? "UNKNOWN",
                TryGetString(issueElement, "type") ?? "UNKNOWN",
                TryGetString(issueElement, "message") ?? "No message provided.",
                TryGetString(issueElement, "component") ?? "unknown",
                TryGetInt32(issueElement, "line"),
                TryGetString(issueElement, "status") ?? "unknown"));
        }

        return issues
            .OrderBy(issue => GetSeverityRank(issue.Severity))
            .ThenBy(issue => issue.Type)
            .ToList();
    }

    public async Task<IReadOnlyList<SonarHotspot>> GetHotspotsAsync(string projectKey, string? branch, int top)
    {
        var queryParts = new List<string>
        {
            $"projectKey={Uri.EscapeDataString(projectKey.Trim())}",
            $"ps={Math.Max(1, top)}"
        };

        if (!string.IsNullOrWhiteSpace(branch))
        {
            queryParts.Add($"branch={Uri.EscapeDataString(branch.Trim())}");
        }

        using var document = await GetJsonAsync($"api/hotspots/search?{string.Join("&", queryParts)}");
        if (!document.RootElement.TryGetProperty("hotspots", out var hotspotsElement))
        {
            return Array.Empty<SonarHotspot>();
        }

        var hotspots = new List<SonarHotspot>();
        foreach (var hotspotElement in hotspotsElement.EnumerateArray())
        {
            hotspots.Add(new SonarHotspot(
                TryGetString(hotspotElement, "key") ?? string.Empty,
                TryGetString(hotspotElement, "securityCategory") ?? "unknown",
                TryGetString(hotspotElement, "vulnerabilityProbability") ?? "unknown",
                TryGetString(hotspotElement, "status") ?? "unknown",
                TryGetString(hotspotElement, "component") ?? "unknown",
                TryGetInt32(hotspotElement, "line"),
                TryGetString(hotspotElement, "message") ?? "No message provided."));
        }

        return hotspots;
    }

    private async Task<(string status, IReadOnlyList<QualityGateCondition> conditions)> GetQualityGateAsync(string projectKey, string? branch)
    {
        var queryParts = new List<string>
        {
            $"projectKey={Uri.EscapeDataString(projectKey.Trim())}"
        };

        if (!string.IsNullOrWhiteSpace(branch))
        {
            queryParts.Add($"branch={Uri.EscapeDataString(branch.Trim())}");
        }

        using var document = await GetJsonAsync($"api/qualitygates/project_status?{string.Join("&", queryParts)}");
        if (!document.RootElement.TryGetProperty("projectStatus", out var projectStatus))
        {
            return ("unknown", Array.Empty<QualityGateCondition>());
        }

        var conditions = new List<QualityGateCondition>();
        if (projectStatus.TryGetProperty("conditions", out var conditionsElement))
        {
            foreach (var conditionElement in conditionsElement.EnumerateArray())
            {
                conditions.Add(new QualityGateCondition(
                    TryGetString(conditionElement, "metricKey") ?? "unknown",
                    TryGetString(conditionElement, "status") ?? "unknown",
                    TryGetString(conditionElement, "actualValue"),
                    TryGetString(conditionElement, "errorThreshold")));
            }
        }

        return (TryGetString(projectStatus, "status") ?? "unknown", conditions);
    }

    private async Task<JsonDocument> GetJsonAsync(string relativePath)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync(relativePath);
        using var stream = await response.Content.ReadAsStreamAsync();
        if (!response.IsSuccessStatusCode)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            throw new InvalidOperationException($"SonarQube request failed with HTTP {(int)response.StatusCode}: {body}");
        }

        return await JsonDocument.ParseAsync(stream);
    }

    private HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl + "/")
        };

        var tokenBytes = Encoding.ASCII.GetBytes($"{Token}:");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(tokenBytes));
        return client;
    }

    private static string SimplifyComponent(string component)
    {
        if (string.IsNullOrWhiteSpace(component))
        {
            return "unknown";
        }

        var separatorIndex = component.IndexOf(':');
        return separatorIndex >= 0 && separatorIndex < component.Length - 1
            ? component[(separatorIndex + 1)..]
            : component;
    }

    private static int GetSeverityRank(string severity) => severity.ToUpperInvariant() switch
    {
        "BLOCKER" => 0,
        "CRITICAL" => 1,
        "MAJOR" => 2,
        "MINOR" => 3,
        "INFO" => 4,
        _ => 5
    };

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) ? property.ToString() : null;
    }

    private static int? TryGetInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }
}