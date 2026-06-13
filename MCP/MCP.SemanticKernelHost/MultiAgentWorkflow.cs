using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace MCP.SemanticKernelHost;

internal sealed class MultiAgentWorkflow(Kernel kernel, string pluginName, ILogger<MultiAgentWorkflow> logger)
{
    private readonly Kernel _kernel = kernel;
    private readonly string _pluginName = pluginName;
    private readonly ILogger<MultiAgentWorkflow> _logger = logger;

    public async Task<WorkflowResult> RunAsync(string featureRequest, CancellationToken cancellationToken = default)
    {
        var transcript = new List<WorkflowTurn>
        {
            new("User", featureRequest)
        };

        var keywordQuery = BuildKeywordQuery(featureRequest);
        var titleResults = await InvokeToolAsync("ResearchAgent", "architecture_search_titles", new KernelArguments
        {
            ["query"] = keywordQuery
        }, cancellationToken);
        transcript.Add(new WorkflowTurn("ResearchAgent", $"Relevant architecture titles:\n{titleResults}"));

        var hybridResults = await InvokeToolAsync("ResearchAgent", "architecture_search_hybrid", new KernelArguments
        {
            ["query"] = featureRequest
        }, cancellationToken);
        transcript.Add(new WorkflowTurn("ResearchAgent", $"Relevant architecture passages:\n{hybridResults}"));

        string backlogContext;
        if (TryExtractWorkItemId(featureRequest, out var workItemId))
        {
            backlogContext = await InvokeToolAsync("DeliveryAgent", "azure_devops_get_work_item", new KernelArguments
            {
                ["workItemId"] = workItemId
            }, cancellationToken);
        }
        else
        {
            backlogContext = "No work item reference was provided, so Azure DevOps lookup was skipped.";
            _logger.LogInformation("DeliveryAgent skipped azure_devops_get_work_item because no work item ID was found in the request.");
        }

        transcript.Add(new WorkflowTurn("DeliveryAgent", backlogContext));

        var finalResponse = $"Research summary:\n{hybridResults}\n\nBacklog context:\n{backlogContext}\n\nRecommendation: use the architecture search results to refine the feature scope, then align implementation details with the referenced Azure DevOps work item before planning development.";

        return new WorkflowResult(transcript, finalResponse);
    }

    private async Task<string> InvokeToolAsync(string agentName, string functionName, KernelArguments arguments, CancellationToken cancellationToken)
    {
        if (!_kernel.Plugins.TryGetFunction(_pluginName, functionName, out var function) || function is null)
        {
            throw new InvalidOperationException($"Kernel function '{_pluginName}.{functionName}' is not registered.");
        }

        var argumentSummary = string.Join(", ", arguments.Select(argument => $"{argument.Key}={argument.Value}"));
        _logger.LogInformation("{Agent} calling {Plugin}.{Function} with {Arguments}", agentName, _pluginName, functionName, argumentSummary);

        var result = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        var text = result.ToString();

        _logger.LogInformation("{Agent} completed {Plugin}.{Function} with result: {Result}", agentName, _pluginName, functionName, text);

        return text;
    }

    private static string BuildKeywordQuery(string featureRequest)
    {
        var tokens = featureRequest
            .Split([' ', ',', '.', ':', ';', '?', '!'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 3 && !int.TryParse(token, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToArray();

        return tokens.Length == 0 ? featureRequest : string.Join(' ', tokens);
    }

    private static bool TryExtractWorkItemId(string text, out int workItemId)
    {
        var match = Regex.Match(text, @"(?:work item|backlog item|bug|feature)\s+#?(\d+)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out workItemId))
        {
            return true;
        }

        workItemId = 0;
        return false;
    }
}

internal sealed record WorkflowTurn(string Agent, string Message);

internal sealed record WorkflowResult(IReadOnlyList<WorkflowTurn> Transcript, string FinalResponse);

internal sealed class ConsoleWorkflowLogger<T> : ILogger<T>
{
    IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

    bool ILogger.IsEnabled(LogLevel logLevel) => true;

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"{DateTimeOffset.Now:HH:mm:ss} [{logLevel}] {formatter(state, exception)}");
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}