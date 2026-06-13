namespace MCP_POC1.Shared;

internal static class EnvFileLoader
{
    public static void LoadFromCurrentDirectory(string fileName = ".env")
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, fileName);
        if (!File.Exists(filePath))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var name = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            {
                continue;
            }

            Environment.SetEnvironmentVariable(name, TrimWrappingQuotes(value));
        }
    }

    private static string TrimWrappingQuotes(string value)
    {
        if (value.Length >= 2)
        {
            var first = value[0];
            var last = value[^1];
            if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
            {
                return value[1..^1];
            }
        }

        return value;
    }
}