using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace MCP.SemanticKernelHost;

internal sealed class McpSemanticKernelPluginAdapter
{
    public async Task<KernelPlugin> CreatePluginAsync(McpClient client, string pluginName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
        var functions = tools.Select(tool => CreateKernelFunction(client, tool)).ToArray();

        return KernelPluginFactory.CreateFromFunctions(
            pluginName,
            "Tools imported from the MCP server.",
            functions);
    }

    private static KernelFunction CreateKernelFunction(McpClient client, McpClientTool tool)
    {
        return KernelFunctionFactory.CreateFromMethod(
            (KernelArguments arguments, CancellationToken cancellationToken) => InvokeToolAsync(client, tool.Name, arguments, cancellationToken),
            tool.Name,
            tool.Description,
            [],
            new KernelReturnParameterMetadata
            {
                Description = "Raw text content returned by the MCP tool.",
                ParameterType = typeof(string)
            },
            null);
    }

    private static async Task<string> InvokeToolAsync(McpClient client, string toolName, KernelArguments arguments, CancellationToken cancellationToken)
    {
        var toolArguments = arguments.ToDictionary(argument => argument.Key, argument => argument.Value);
        var result = await client.CallToolAsync(toolName, toolArguments, cancellationToken: cancellationToken);

        return string.Join(
            Environment.NewLine,
            result.Content.OfType<TextContentBlock>().Select(content => content.Text));
    }
}