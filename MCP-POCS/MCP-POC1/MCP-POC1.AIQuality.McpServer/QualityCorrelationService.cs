using System.Text;

namespace MCP_POC1.AIQuality.McpServer;

internal static class QualityCorrelationService
{
    public static async Task<string> BuildRootCauseAnalysisAsync(
        SonarQubeQualityClient sonarClient,
        ApplicationInsightsQualityClient appInsightsClient,
        AzureDevOpsQualityClient azureDevOpsClient,
        string projectKey,
        string? repositoryName,
        string? exceptionKeyword,
        int daysBack,
        int? pullRequestId)
    {
        var report = await BuildRootCauseReportAsync(
            sonarClient,
            appInsightsClient,
            azureDevOpsClient,
            projectKey,
            repositoryName,
            exceptionKeyword,
            daysBack,
            pullRequestId);

        return FormatRootCauseReport(report);
    }

    public static async Task<RootCauseReport> BuildRootCauseReportAsync(
        SonarQubeQualityClient sonarClient,
        ApplicationInsightsQualityClient appInsightsClient,
        AzureDevOpsQualityClient azureDevOpsClient,
        string projectKey,
        string? repositoryName,
        string? exceptionKeyword,
        int daysBack,
        int? pullRequestId)
    {
        var failureSignals = await appInsightsClient.GetFailureSignalsAsync(daysBack, cloudRoleName: null, top: 10);
        var primaryFailure = SelectPrimaryFailure(failureSignals, exceptionKeyword);
        var sonarIssues = await sonarClient.GetIssuesAsync(projectKey, branch: null, top: 50);

        PullRequestContext? pullRequestContext = null;
        var resolvedRepository = string.IsNullOrWhiteSpace(repositoryName) ? azureDevOpsClient.DefaultRepository : repositoryName.Trim();
        if (!string.IsNullOrWhiteSpace(resolvedRepository) && pullRequestId.HasValue)
        {
            pullRequestContext = await azureDevOpsClient.GetPullRequestContextAsync(resolvedRepository, pullRequestId.Value);
        }

        var matchedIssues = MatchIssues(primaryFailure, sonarIssues, pullRequestContext).Take(5).ToList();
        var impactedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (pullRequestContext is not null)
        {
            foreach (var file in pullRequestContext.ChangedFiles.Take(10))
            {
                impactedFiles.Add(file);
            }
        }

        foreach (var issue in matchedIssues)
        {
            impactedFiles.Add(SimplifyComponent(issue.Component));
        }

        var priority = DeterminePriority(primaryFailure, matchedIssues);
        var confidence = DetermineConfidence(primaryFailure, matchedIssues, pullRequestContext);

        var title = primaryFailure?.ExceptionType
            ?? matchedIssues.FirstOrDefault()?.Message
            ?? $"{projectKey} quality correlation";
        var summary = BuildRootCauseSummary(primaryFailure, matchedIssues, impactedFiles);
        var recommendations = BuildRecommendations(primaryFailure, matchedIssues).ToList();

        return new RootCauseReport(
            title,
            summary,
            priority,
            confidence,
            primaryFailure,
            pullRequestContext,
            impactedFiles.Take(8).ToList(),
            matchedIssues,
            recommendations);
    }

    public static string FormatRootCauseReport(RootCauseReport report)
    {

        var lines = new List<string>
        {
            "Root Cause Report",
            $"Priority: {report.Priority}",
            $"Confidence: {report.Confidence}"
        };

        if (report.PrimaryFailure is null)
        {
            lines.Add("No dominant Application Insights exception group was found for the requested time window.");
        }
        else
        {
            lines.Add($"Runtime signal: {report.PrimaryFailure.ExceptionType}");
            lines.Add($"Occurrences: {report.PrimaryFailure.Occurrences}");
            lines.Add($"Operation: {report.PrimaryFailure.OperationName}");
            lines.Add($"Message: {report.PrimaryFailure.Message}");
        }

        if (report.PullRequestContext is not null)
        {
            lines.Add($"Pull request context: #{report.PullRequestContext.PullRequestId} {report.PullRequestContext.Title}");
            lines.Add($"Changed files in PR: {report.PullRequestContext.ChangedFiles.Count}");
        }

        if (report.ImpactedFiles.Count > 0)
        {
            lines.Add("Impacted files:");
            foreach (var file in report.ImpactedFiles)
            {
                lines.Add($"- {file}");
            }
        }

        if (report.MatchedIssues.Count > 0)
        {
            lines.Add("Related SonarQube issues:");
            foreach (var issue in report.MatchedIssues)
            {
                lines.Add($"- [{issue.Severity}] {issue.Message} | File: {SimplifyComponent(issue.Component)} | Rule: {issue.Rule}");
            }
        }
        else
        {
            lines.Add("No strongly matching SonarQube issues were found. Review the runtime failure and changed files directly.");
        }

        lines.Add("Recommended fix:");
        foreach (var recommendation in report.Recommendations)
        {
            lines.Add($"- {recommendation}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public static RemediationPlan BuildRemediationPlan(RootCauseReport report)
    {
        var plan = BuildRemediationPlan(report.Title, report.Summary);
        var enrichedActions = plan.Actions.ToList();

        if (report.MatchedIssues.Any(issue => string.Equals(issue.Type, "VULNERABILITY", StringComparison.OrdinalIgnoreCase)))
        {
            enrichedActions.Add(new RemediationAction(
                $"Validate secure release for {report.Title}",
                "Confirm the remediation is deployed with secure configuration, smoke tests, and post-release monitoring in place.",
                "Release"));
        }

        var enrichedRiskNotes = plan.RiskNotes.ToList();
        if (report.PullRequestContext is not null)
        {
            enrichedRiskNotes.Add($"Review PR #{report.PullRequestContext.PullRequestId} and its linked work items before closing the remediation hierarchy.");
        }

        return plan with
        {
            Summary = report.Summary,
            Priority = report.Priority,
            RiskNotes = enrichedRiskNotes.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            Actions = enrichedActions
        };
    }

    public static RemediationPlan BuildRemediationPlan(string title, string analysisSummary)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? "AI quality remediation" : title.Trim();
        var normalizedSummary = string.IsNullOrWhiteSpace(analysisSummary)
            ? "Review the correlated runtime failure, static analysis signals, and changed files before implementing the fix."
            : analysisSummary.Trim();

        var priority = ContainsAny(normalizedSummary, "critical", "blocker", "outage", "sev1")
            ? "High"
            : ContainsAny(normalizedSummary, "major", "degraded", "warning")
                ? "Medium"
                : "Low";

        var effort = normalizedSummary.Length > 700 || ContainsAny(normalizedSummary, "multiple files", "cross-service", "security")
            ? "Medium"
            : "Small";

        var actions = new List<RemediationAction>
        {
            new(
                $"Investigate {normalizedTitle}",
                "Reproduce the failure path, confirm the failing request, and validate whether the issue is confined to one code path or spans multiple services.",
                "Investigation"),
            new(
                $"Implement fix for {normalizedTitle}",
                "Apply the code fix, add defensive checks or input validation where needed, and remove the direct cause identified in runtime and SonarQube signals.",
                "Implementation"),
            new(
                $"Add regression protection for {normalizedTitle}",
                "Add or update automated tests, dashboards, and alerting so the same issue is detected earlier next time.",
                "Validation")
        };

        if (ContainsAny(normalizedSummary, "sql injection", "secret", "vulnerability", "security"))
        {
            actions.Insert(2, new RemediationAction(
                $"Harden security controls for {normalizedTitle}",
                "Review secrets, query construction, permissions, and code scanning rules related to the issue before closing the remediation work.",
                "Security"));
        }

        var riskNotes = new List<string>
        {
            "If the runtime signal is still active, validate rollback steps before deployment.",
            "Confirm telemetry, alerts, and dashboards are updated together with the code change."
        };

        if (ContainsAny(normalizedSummary, "security", "vulnerability", "secret", "sql injection"))
        {
            riskNotes.Insert(0, "Security findings should be treated as release blockers until the remediation is verified.");
        }

        var acceptanceCriteria = string.Join(Environment.NewLine, new[]
        {
            "1. The root cause is confirmed with code and telemetry evidence.",
            "2. The code fix removes the runtime failure or high-risk quality signal.",
            "3. Automated tests or verification steps cover the corrected path.",
            "4. Monitoring and alerting are updated or validated after the fix."
        });

        var validationPlan = string.Join(Environment.NewLine, new[]
        {
            "- Re-run the failing scenario in a lower environment.",
            "- Confirm SonarQube findings or quality gate status improve as expected.",
            "- Confirm Application Insights shows reduced or eliminated failure volume after deployment."
        });

        return new RemediationPlan(
            normalizedTitle,
            normalizedSummary,
            priority,
            effort,
            $"AI Quality Governance - {normalizedTitle}",
            $"Quality Remediation - {normalizedTitle}",
            $"Resolve {normalizedTitle}",
            acceptanceCriteria,
            validationPlan,
            riskNotes,
            actions);
    }

    public static async Task<string> BuildPullRequestReviewAsync(
        AzureDevOpsQualityClient azureDevOpsClient,
        string repositoryName,
        int pullRequestId,
        SonarQubeQualityClient? sonarClient,
        string? projectKey,
        ApplicationInsightsQualityClient? appInsightsClient,
        int daysBack)
    {
        var pullRequest = await azureDevOpsClient.GetPullRequestContextAsync(repositoryName, pullRequestId);
        var matchingIssues = new List<SonarIssue>();
        if (sonarClient is not null && !string.IsNullOrWhiteSpace(projectKey))
        {
            var sonarIssues = await sonarClient.GetIssuesAsync(projectKey.Trim(), branch: null, top: 100);
            matchingIssues = sonarIssues
                .Where(issue => pullRequest.ChangedFiles.Any(file => ComponentMatchesFile(issue.Component, file)))
                .Take(10)
                .ToList();
        }

        IReadOnlyList<FailureSignal> failureSignals = Array.Empty<FailureSignal>();
        if (appInsightsClient is not null)
        {
            failureSignals = await appInsightsClient.GetFailureSignalsAsync(daysBack, cloudRoleName: null, top: 5);
        }

        var lines = new List<string>
        {
            $"AI quality review for PR #{pullRequest.PullRequestId}",
            $"Title: {pullRequest.Title}",
            $"Status: {pullRequest.Status}",
            $"Changed files: {pullRequest.ChangedFiles.Count}"
        };

        var riskFlags = new List<string>();
        if (pullRequest.ChangedFiles.Count >= 15)
        {
            riskFlags.Add("The PR touches many files, which increases review and regression risk.");
        }

        if (matchingIssues.Any(issue => IsHighSeverity(issue.Severity)))
        {
            riskFlags.Add("The changed files overlap with high-severity SonarQube findings.");
        }

        if (matchingIssues.Any(issue => string.Equals(issue.Type, "VULNERABILITY", StringComparison.OrdinalIgnoreCase)))
        {
            riskFlags.Add("The PR overlaps with security-related SonarQube issues and needs focused review on safe coding patterns.");
        }

        if (failureSignals.Count > 0)
        {
            riskFlags.Add("Recent runtime failures exist in the environment, so this PR should be checked for operational side effects and rollback safety.");
        }

        if (riskFlags.Count == 0)
        {
            riskFlags.Add("No major automated risk flags were detected from the currently available data sources.");
        }

        lines.Add("Review guidance:");
        foreach (var riskFlag in riskFlags)
        {
            lines.Add($"- {riskFlag}");
        }

        if (matchingIssues.Count > 0)
        {
            lines.Add("Relevant SonarQube findings in changed files:");
            foreach (var issue in matchingIssues)
            {
                lines.Add($"- [{issue.Severity}] {issue.Message} | File: {SimplifyComponent(issue.Component)} | Rule: {issue.Rule}");
            }
        }

        if (failureSignals.Count > 0)
        {
            lines.Add("Recent runtime signals to keep in mind:");
            foreach (var failure in failureSignals.Take(3))
            {
                lines.Add($"- {failure.ExceptionType} in {failure.OperationName} | Occurrences: {failure.Occurrences}");
            }
        }

        lines.Add("Recommended reviewer focus:");
        lines.Add("- Validate null handling, input validation, and error boundaries in the changed files.");
        lines.Add("- Check whether tests cover the changed branches and whether monitoring or alerts need updates.");
        lines.Add("- Confirm linked work items and release notes reflect the operational risk when runtime failures are already present.");

        return string.Join(Environment.NewLine, lines);
    }

    private static FailureSignal? SelectPrimaryFailure(IEnumerable<FailureSignal> signals, string? exceptionKeyword)
    {
        var filteredSignals = signals;
        if (!string.IsNullOrWhiteSpace(exceptionKeyword))
        {
            filteredSignals = filteredSignals.Where(signal =>
                ContainsIgnoreCase(signal.ExceptionType, exceptionKeyword)
                || ContainsIgnoreCase(signal.Message, exceptionKeyword)
                || ContainsIgnoreCase(signal.OperationName, exceptionKeyword));
        }

        return filteredSignals.OrderByDescending(signal => signal.Occurrences).FirstOrDefault();
    }

    private static IEnumerable<SonarIssue> MatchIssues(FailureSignal? primaryFailure, IReadOnlyList<SonarIssue> issues, PullRequestContext? pullRequestContext)
    {
        if (issues.Count == 0)
        {
            return Array.Empty<SonarIssue>();
        }

        var matchTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (primaryFailure is not null)
        {
            foreach (var token in ExtractMatchTokens(primaryFailure.ExceptionType, primaryFailure.Message, primaryFailure.OperationName, primaryFailure.Method))
            {
                matchTokens.Add(token);
            }
        }

        if (pullRequestContext is not null)
        {
            foreach (var file in pullRequestContext.ChangedFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    matchTokens.Add(fileName);
                }
            }
        }

        var matched = issues
            .Where(issue => matchTokens.Count == 0 || matchTokens.Any(token =>
                ContainsIgnoreCase(issue.Message, token)
                || ContainsIgnoreCase(issue.Rule, token)
                || ContainsIgnoreCase(issue.Component, token)))
            .OrderBy(issue => GetSeverityRank(issue.Severity))
            .ThenBy(issue => issue.Type)
            .ToList();

        if (matched.Count == 0 && pullRequestContext is not null)
        {
            matched = issues
                .Where(issue => pullRequestContext.ChangedFiles.Any(file => ComponentMatchesFile(issue.Component, file)))
                .OrderBy(issue => GetSeverityRank(issue.Severity))
                .ThenBy(issue => issue.Type)
                .ToList();
        }

        return matched;
    }

    private static IEnumerable<string> BuildRecommendations(FailureSignal? primaryFailure, IReadOnlyList<SonarIssue> matchedIssues)
    {
        var recommendations = new List<string>();
        if (primaryFailure is not null)
        {
            if (ContainsAny(primaryFailure.ExceptionType, "null", "argumentnull"))
            {
                recommendations.Add("Add guard clauses and validate configuration or request objects before the failing code path is executed.");
            }

            recommendations.Add("Reproduce the failing request in a lower environment and capture the exact input and dependency behavior.");
        }

        if (matchedIssues.Any(issue => string.Equals(issue.Type, "VULNERABILITY", StringComparison.OrdinalIgnoreCase)))
        {
            recommendations.Add("Treat the related vulnerability findings as release blockers until the secure coding change is verified.");
        }

        if (matchedIssues.Any(issue => ContainsAny(issue.Message, "null", "dereference")))
        {
            recommendations.Add("Review nullable reference usage and add explicit validation where SonarQube indicates possible null dereference behavior.");
        }

        recommendations.Add("Add or update regression tests and alerting tied to the corrected code path before closing the issue.");
        return recommendations.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildRootCauseSummary(FailureSignal? primaryFailure, IReadOnlyList<SonarIssue> matchedIssues, IReadOnlyCollection<string> impactedFiles)
    {
        var parts = new List<string>();

        if (primaryFailure is not null)
        {
            parts.Add($"Runtime failure '{primaryFailure.ExceptionType}' occurred {primaryFailure.Occurrences} time(s) in '{primaryFailure.OperationName}'.");
        }

        if (matchedIssues.Count > 0)
        {
            parts.Add($"{matchedIssues.Count} correlated SonarQube issue(s) were identified.");
        }

        if (impactedFiles.Count > 0)
        {
            parts.Add($"Impacted files include {string.Join(", ", impactedFiles.Take(3))}.");
        }

        return parts.Count > 0
            ? string.Join(" ", parts)
            : "Static analysis and runtime telemetry need manual triage because no strong cross-source correlation was found.";
    }

    private static string DeterminePriority(FailureSignal? primaryFailure, IReadOnlyList<SonarIssue> matchedIssues)
    {
        if (primaryFailure is not null && primaryFailure.Occurrences >= 100)
        {
            return "High";
        }

        if (matchedIssues.Any(issue => IsHighSeverity(issue.Severity)))
        {
            return "High";
        }

        return primaryFailure is not null || matchedIssues.Count > 0 ? "Medium" : "Low";
    }

    private static string DetermineConfidence(FailureSignal? primaryFailure, IReadOnlyList<SonarIssue> matchedIssues, PullRequestContext? pullRequestContext)
    {
        if (primaryFailure is not null && matchedIssues.Count > 0 && pullRequestContext is not null)
        {
            return "High";
        }

        if (primaryFailure is not null || matchedIssues.Count > 0)
        {
            return "Medium";
        }

        return "Low";
    }

    private static IEnumerable<string> ExtractMatchTokens(params string?[] values)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var token in value.Split(new[] { ' ', '.', ':', '/', '\\', '-', '_', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (token.Length >= 4)
                {
                    yield return token;
                }
            }
        }
    }

    private static bool ComponentMatchesFile(string component, string file)
    {
        var componentPath = SimplifyComponent(component);
        return componentPath.EndsWith(file.TrimStart('/'), StringComparison.OrdinalIgnoreCase)
            || componentPath.EndsWith(Path.GetFileName(file), StringComparison.OrdinalIgnoreCase);
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

    private static bool IsHighSeverity(string severity) => GetSeverityRank(severity) <= 1;

    private static int GetSeverityRank(string severity) => severity.ToUpperInvariant() switch
    {
        "BLOCKER" => 0,
        "CRITICAL" => 1,
        "MAJOR" => 2,
        "MINOR" => 3,
        "INFO" => 4,
        _ => 5
    };

    private static bool ContainsIgnoreCase(string value, string candidate) =>
        value.Contains(candidate, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsAny(string value, params string[] candidates) =>
        candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
}