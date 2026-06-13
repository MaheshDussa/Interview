using MCP.SemanticKernelHost;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

var mode = args.FirstOrDefault()?.Equals("workflow", StringComparison.OrdinalIgnoreCase) == true ? "workflow" : "list";
var endpointArgumentIndex = mode == "workflow" ? 2 : 0;
var endpoint = new Uri(args.ElementAtOrDefault(endpointArgumentIndex) ?? "http://localhost:5007/mcp");
var transport = new HttpClientTransport(new HttpClientTransportOptions
{
	Endpoint = endpoint,
	TransportMode = HttpTransportMode.StreamableHttp
});

await using var client = await McpClient.CreateAsync(transport);

var adapter = new McpSemanticKernelPluginAdapter();
var plugin = await adapter.CreatePluginAsync(client, "custom_mcp_tools");

var builder = Kernel.CreateBuilder();
builder.Plugins.Add(plugin);

var kernel = builder.Build();
var functions = kernel.Plugins.GetFunctionsMetadata().OrderBy(function => function.PluginName).ThenBy(function => function.Name).ToArray();

if (mode == "workflow")
{
	var request = args.ElementAtOrDefault(1) ?? "We need a better secure architecture indexing experience. Please check work item 108 and the related gateway/search pipeline docs.";
	var workflow = new MultiAgentWorkflow(kernel, plugin.Name, new ConsoleWorkflowLogger<MultiAgentWorkflow>());
	var result = await workflow.RunAsync(request);

	Console.WriteLine($"Connected to MCP server at {endpoint}");
	Console.WriteLine("Initialization completed successfully.");
	Console.WriteLine("Workflow transcript:");

	foreach (var turn in result.Transcript)
	{
		Console.WriteLine($"- {turn.Agent}: {turn.Message}");
	}

	Console.WriteLine("Final response:");
	Console.WriteLine(result.FinalResponse);
	return;
}

Console.WriteLine($"Connected to MCP server at {endpoint}");
Console.WriteLine("Initialization completed successfully.");
Console.WriteLine("Registered Semantic Kernel functions:");

foreach (var function in functions)
{
	Console.WriteLine($"- {function.PluginName}.{function.Name}: {function.Description}");
}
