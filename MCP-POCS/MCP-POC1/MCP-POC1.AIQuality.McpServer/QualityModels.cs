namespace MCP_POC1.AIQuality.McpServer;

internal sealed record SonarIssue(
    string Key,
    string Rule,
    string Severity,
    string Type,
    string Message,
    string Component,
    int? Line,
    string Status);

internal sealed record SonarHotspot(
    string Key,
    string SecurityCategory,
    string VulnerabilityProbability,
    string Status,
    string Component,
    int? Line,
    string Message);

internal sealed record QualityGateCondition(
    string Metric,
    string Status,
    string? ActualValue,
    string? Threshold);

internal sealed record FailureSignal(
    string ExceptionType,
    string Message,
    string OperationName,
    string? Method,
    string? ProblemId,
    int Occurrences);

internal sealed record PullRequestContext(
    int PullRequestId,
    string Title,
    string Status,
    string SourceBranch,
    string TargetBranch,
    IReadOnlyList<string> ChangedFiles,
    IReadOnlyList<int> LinkedWorkItemIds);

internal sealed record RootCauseReport(
    string Title,
    string Summary,
    string Priority,
    string Confidence,
    FailureSignal? PrimaryFailure,
    PullRequestContext? PullRequestContext,
    IReadOnlyList<string> ImpactedFiles,
    IReadOnlyList<SonarIssue> MatchedIssues,
    IReadOnlyList<string> Recommendations);

internal sealed record RemediationAction(
    string Title,
    string Description,
    string Category);

internal sealed record RemediationPlan(
    string Title,
    string Summary,
    string Priority,
    string Effort,
    string EpicTitle,
    string FeatureTitle,
    string StoryTitle,
    string AcceptanceCriteria,
    string ValidationPlan,
    IReadOnlyList<string> RiskNotes,
    IReadOnlyList<RemediationAction> Actions);