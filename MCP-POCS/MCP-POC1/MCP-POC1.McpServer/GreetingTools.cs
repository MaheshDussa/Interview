using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.McpServer;

[McpServerToolType]
public static class GreetingTools
{
    [McpServerTool, Description("Returns a friendly greeting for the provided name.")]
    public static string SayHello(
        [Description("The name to greet.")] string name)
    {
        var trimmedName = string.IsNullOrWhiteSpace(name) ? "friend" : name.Trim();
        return $"Hello, {trimmedName}. Greetings from the MCP server.";
    }
}