using Azure.Core;
using Azure.Identity;
using MCP_POC1.Shared;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MCP_POC1.AIQuality.McpServer;

internal sealed class ApplicationInsightsQualityClient
{
    private const string AppInsightsScope = "https://api.applicationinsights.io/.default";

    public static bool TryCreate(out ApplicationInsightsQualityClient? client, out string configurationMessage)
    {
        var appId = EnvironmentSettings.ReadFirst("APPINSIGHTS_APP_ID");
        var apiKey = EnvironmentSettings.ReadFirst("APPINSIGHTS_API_KEY");
        var tenantId = EnvironmentSettings.ReadFirst("APPINSIGHTS_TENANT_ID", "AZURE_TENANT_ID");
        var clientId = EnvironmentSettings.ReadFirst("APPINSIGHTS_CLIENT_ID", "AZURE_CLIENT_ID");
        var clientSecret = EnvironmentSettings.ReadFirst("APPINSIGHTS_CLIENT_SECRET", "AZURE_CLIENT_SECRET");

        var missingSettings = new List<string>();
        if (string.IsNullOrWhiteSpace(appId))
        {
            missingSettings.Add("APPINSIGHTS_APP_ID");
        }

        var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
        var hasExplicitServicePrincipal = !string.IsNullOrWhiteSpace(tenantId)
            && !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret);

        if (!hasApiKey && !hasExplicitServicePrincipal)
        {
            missingSettings.Add("APPINSIGHTS_API_KEY or Azure AD credentials");
        }

        if (missingSettings.Count > 0)
        {
            client = null;
            configurationMessage = "Application Insights MCP tools are not configured yet. Set these environment variables: "
                + string.Join(", ", missingSettings)
                + ". Preferred auth is Azure AD via APPINSIGHTS_TENANT_ID, APPINSIGHTS_CLIENT_ID, and APPINSIGHTS_CLIENT_SECRET (or AZURE_* equivalents).";
            return false;
        }

        client = new ApplicationInsightsQualityClient(appId!, apiKey, tenantId, clientId, clientSecret);
        configurationMessage = string.Empty;
        return true;
    }

    private ApplicationInsightsQualityClient(string appId, string? apiKey, string? tenantId, string? clientId, string? clientSecret)
    {
        AppId = appId;
        ApiKey = apiKey;
        Credential = CreateCredential(tenantId, clientId, clientSecret);
    }

    private string AppId { get; }

    private string? ApiKey { get; }

    private TokenCredential? Credential { get; }

    public async Task<string> GetFailureSummaryAsync(int daysBack, string? cloudRoleName, int top)
    {
        var requestStats = await GetRequestStatsAsync(daysBack, cloudRoleName);
        var dependencyFailures = await GetDependencyFailuresAsync(daysBack, cloudRoleName, top: 5);
        var failures = await GetFailureSignalsAsync(daysBack, cloudRoleName, top);

        var lines = new List<string>
        {
            $"Application Insights failure summary for the last {Math.Abs(daysBack)} day(s)",
            $"Requests: {requestStats.total} total, {requestStats.failed} failed"
        };

        if (!string.IsNullOrWhiteSpace(cloudRoleName))
        {
            lines.Add($"Filtered cloud role: {cloudRoleName.Trim()}");
        }

        if (dependencyFailures.Count > 0)
        {
            lines.Add("Dependency failures:");
            foreach (var dependencyFailure in dependencyFailures)
            {
                lines.Add($"- {dependencyFailure.target}: {dependencyFailure.failures} failure(s)");
            }
        }

        if (failures.Count == 0)
        {
            lines.Add("No exception groups were returned by Application Insights.");
            return string.Join(Environment.NewLine, lines);
        }

        lines.Add("Top exception groups:");
        foreach (var failure in failures)
        {
            lines.Add($"- {failure.ExceptionType} | Operation: {failure.OperationName} | Occurrences: {failure.Occurrences} | Message: {failure.Message}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task<IReadOnlyList<FailureSignal>> GetFailureSignalsAsync(int daysBack, string? cloudRoleName, int top)
    {
        var queryBuilder = new List<string>
        {
            "exceptions",
            $"| where timestamp > ago({Math.Abs(daysBack)}d)"
        };

        if (!string.IsNullOrWhiteSpace(cloudRoleName))
        {
            queryBuilder.Add($"| where cloud_RoleName == '{EscapeKqlLiteral(cloudRoleName.Trim())}'");
        }

        queryBuilder.Add("| summarize occurrences=count() by type, outerMessage, operation_Name, method, problemId");
        queryBuilder.Add($"| top {Math.Max(1, top)} by occurrences desc");

        using var document = await RunQueryAsync(string.Join(Environment.NewLine, queryBuilder));
        var rows = ReadRows(document);

        return rows.Select(row => new FailureSignal(
                row.TryGetValue("type", out var exceptionType) ? exceptionType : "UnknownException",
                row.TryGetValue("outerMessage", out var message) && !string.IsNullOrWhiteSpace(message) ? message : "No message provided.",
                row.TryGetValue("operation_Name", out var operationName) && !string.IsNullOrWhiteSpace(operationName) ? operationName : "unknown",
                row.TryGetValue("method", out var method) ? method : null,
                row.TryGetValue("problemId", out var problemId) ? problemId : null,
                row.TryGetValue("occurrences", out var occurrencesText) && int.TryParse(occurrencesText, out var occurrences) ? occurrences : 0))
            .Where(signal => signal.Occurrences > 0)
            .ToList();
    }

    private async Task<(int total, int failed)> GetRequestStatsAsync(int daysBack, string? cloudRoleName)
    {
        var queryBuilder = new List<string>
        {
            "requests",
            $"| where timestamp > ago({Math.Abs(daysBack)}d)"
        };

        if (!string.IsNullOrWhiteSpace(cloudRoleName))
        {
            queryBuilder.Add($"| where cloud_RoleName == '{EscapeKqlLiteral(cloudRoleName.Trim())}'");
        }

        queryBuilder.Add("| summarize total=count(), failed=countif(success == false)");

        using var document = await RunQueryAsync(string.Join(Environment.NewLine, queryBuilder));
        var rows = ReadRows(document);
        if (rows.Count == 0)
        {
            return (0, 0);
        }

        var row = rows[0];
        return (
            row.TryGetValue("total", out var totalText) && int.TryParse(totalText, out var total) ? total : 0,
            row.TryGetValue("failed", out var failedText) && int.TryParse(failedText, out var failed) ? failed : 0);
    }

    private async Task<IReadOnlyList<(string target, int failures)>> GetDependencyFailuresAsync(int daysBack, string? cloudRoleName, int top)
    {
        var queryBuilder = new List<string>
        {
            "dependencies",
            $"| where timestamp > ago({Math.Abs(daysBack)}d)",
            "| where success == false"
        };

        if (!string.IsNullOrWhiteSpace(cloudRoleName))
        {
            queryBuilder.Add($"| where cloud_RoleName == '{EscapeKqlLiteral(cloudRoleName.Trim())}'");
        }

        queryBuilder.Add("| summarize failures=count() by target");
        queryBuilder.Add($"| top {Math.Max(1, top)} by failures desc");

        using var document = await RunQueryAsync(string.Join(Environment.NewLine, queryBuilder));
        var rows = ReadRows(document);

        return rows.Select(row => (
                row.TryGetValue("target", out var target) && !string.IsNullOrWhiteSpace(target) ? target : "unknown",
                row.TryGetValue("failures", out var failuresText) && int.TryParse(failuresText, out var failures) ? failures : 0))
            .ToList();
    }

    private async Task<JsonDocument> RunQueryAsync(string query)
    {
        using var client = new HttpClient();

        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
        }
        else if (Credential is not null)
        {
            var token = await Credential.GetTokenAsync(new TokenRequestContext(new[] { AppInsightsScope }), CancellationToken.None);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        }

        var url = $"https://api.applicationinsights.io/v1/apps/{Uri.EscapeDataString(AppId)}/query?query={Uri.EscapeDataString(query)}";
        using var response = await client.GetAsync(url);
        using var stream = await response.Content.ReadAsStreamAsync();
        if (!response.IsSuccessStatusCode)
        {
            using var reader = new StreamReader(stream);
            var body = await reader.ReadToEndAsync();
            throw new InvalidOperationException($"Application Insights request failed with HTTP {(int)response.StatusCode}: {body}");
        }

        return await JsonDocument.ParseAsync(stream);
    }

    private static List<Dictionary<string, string>> ReadRows(JsonDocument document)
    {
        var results = new List<Dictionary<string, string>>();
        if (!document.RootElement.TryGetProperty("tables", out var tablesElement) || tablesElement.GetArrayLength() == 0)
        {
            return results;
        }

        var table = tablesElement[0];
        if (!table.TryGetProperty("columns", out var columnsElement) || !table.TryGetProperty("rows", out var rowsElement))
        {
            return results;
        }

        var columns = columnsElement.EnumerateArray()
            .Select(column => column.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty)
            .ToList();

        foreach (var rowElement in rowsElement.EnumerateArray())
        {
            var values = rowElement.EnumerateArray().ToArray();
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < columns.Count && index < values.Length; index++)
            {
                row[columns[index]] = values[index].ToString();
            }

            results.Add(row);
        }

        return results;
    }

    private static string EscapeKqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);

    private static TokenCredential? CreateCredential(string? tenantId, string? clientId, string? clientSecret)
    {
        if (!string.IsNullOrWhiteSpace(tenantId)
            && !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret))
        {
            return new ClientSecretCredential(tenantId, clientId, clientSecret);
        }

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            return new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = clientId
            });
        }

        return null;
    }

}