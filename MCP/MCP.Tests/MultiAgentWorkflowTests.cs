using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using MCP.SemanticKernelHost;

namespace MCP.Tests;

public sealed class MultiAgentWorkflowTests
{
    [Fact]
    public async Task RunAsync_LogsToolUsageForVagueFeatureRequest()
    {
        var plugin = KernelPluginFactory.CreateFromFunctions(
            "custom_mcp_tools",
            "Test MCP tools",
            [
                KernelFunctionFactory.CreateFromMethod((string query) => $"Gateway Security Design\nSearch query: {query}", "architecture_search_titles", "Search titles"),
                KernelFunctionFactory.CreateFromMethod((string query) => $"Managed identity secures gateway-to-indexing calls for: {query}", "architecture_search_hybrid", "Hybrid search"),
                KernelFunctionFactory.CreateFromMethod((int workItemId) => $"Work item {workItemId}: Harden search indexing pipeline authentication.", "azure_devops_get_work_item", "Azure DevOps fetch")
            ]);

        var builder = Kernel.CreateBuilder();
        builder.Plugins.Add(plugin);
        var kernel = builder.Build();

        var logger = new TestLogger<MultiAgentWorkflow>();
        var workflow = new MultiAgentWorkflow(kernel, plugin.Name, logger);

        var result = await workflow.RunAsync("We need a better secure architecture indexing experience. Please check work item 108 and the related gateway/search pipeline docs.");

        Assert.Contains(logger.Messages, message => message.Contains("ResearchAgent calling custom_mcp_tools.architecture_search_titles", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("ResearchAgent calling custom_mcp_tools.architecture_search_hybrid", StringComparison.Ordinal));
        Assert.Contains(logger.Messages, message => message.Contains("DeliveryAgent calling custom_mcp_tools.azure_devops_get_work_item", StringComparison.Ordinal));
        Assert.Contains("Managed identity secures gateway-to-indexing calls", result.FinalResponse, StringComparison.Ordinal);
        Assert.Equal(4, result.Transcript.Count);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        IDisposable ILogger.BeginScope<TState>(TState state) => NullScope.Instance;

        bool ILogger.IsEnabled(LogLevel logLevel) => true;

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}