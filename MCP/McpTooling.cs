using System.Reflection;

namespace MCP;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class McpToolAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;

    public string Description { get; } = description;
}

internal sealed record McpRegisteredTool(string Name, string Description, MethodInfo Method);

internal sealed class McpToolDiscoveryService
{
    public IReadOnlyList<McpRegisteredTool> DiscoverTools(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var tools = assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            .Select(method => new
            {
                Method = method,
                Attribute = method.GetCustomAttribute<McpToolAttribute>()
            })
            .Where(entry => entry.Attribute is not null)
            .Select(entry => new McpRegisteredTool(
                entry.Attribute!.Name,
                entry.Attribute.Description,
                entry.Method))
            .OrderBy(tool => tool.Name, StringComparer.Ordinal)
            .ToArray();

        var duplicateNames = tools
            .GroupBy(tool => tool.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateNames.Length > 0)
        {
            throw new InvalidOperationException($"Duplicate MCP tool names found: {string.Join(", ", duplicateNames)}");
        }

        return tools;
    }
}

internal sealed class BuiltInMcpTools
{
    [McpTool("health_check", "Returns the current service health status.")]
    public object GetHealthCheck() => new
    {
        status = "Healthy"
    };
}