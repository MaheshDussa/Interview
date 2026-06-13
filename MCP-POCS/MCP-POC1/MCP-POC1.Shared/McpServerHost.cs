using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace MCP_POC1.Shared;

public static class McpServerHost
{
    public static async Task RunAsync(string[] args)
    {
        EnvFileLoader.LoadFromCurrentDirectory();

        var builder = Host.CreateApplicationBuilder(args);

        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}