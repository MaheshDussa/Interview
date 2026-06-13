using MCP;

namespace MCP.Tests;

public class UnitTest1
{
    [Fact]
    public void DiscoverTools_FindsAndCountsAttributedMethods()
    {
        var discoveryService = new McpToolDiscoveryService();

        var tools = discoveryService.DiscoverTools(typeof(DummyToolContainer).Assembly);

        Assert.Equal(2, tools.Count);
        Assert.Contains(tools, tool => tool.Name == "test_echo");
        Assert.Contains(tools, tool => tool.Name == "test_time");
    }

    private sealed class DummyToolContainer
    {
        [McpTool("test_echo", "Echoes a fixed value for discovery testing.")]
        public string Echo() => "ok";

        [McpTool("test_time", "Returns a timestamp for discovery testing.")]
        private static DateTimeOffset GetTimestamp() => DateTimeOffset.UtcNow;

        public void IgnoredMethod()
        {
        }
    }
}
