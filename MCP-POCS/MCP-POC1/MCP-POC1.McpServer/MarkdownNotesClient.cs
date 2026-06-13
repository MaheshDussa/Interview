using MCP_POC1.Shared;
using System.Text;

namespace MCP_POC1.McpServer;

internal sealed class MarkdownNotesClient
{
    public static MarkdownNotesClient Create()
    {
        var outputDirectory = EnvironmentSettings.ReadFirst("LEARNING_NOTES_OUTPUT_DIR", "MARKDOWN_NOTES_OUTPUT_DIR");
        outputDirectory ??= Path.Combine(Environment.CurrentDirectory, "LearningNotes");
        return new MarkdownNotesClient(outputDirectory);
    }

    private MarkdownNotesClient(string outputDirectory)
    {
        OutputDirectory = outputDirectory;
    }

    private string OutputDirectory { get; }

    public async Task<string> SaveNotesAsync(OneNoteNotesClient.GeneratedNotes notes)
    {
        Directory.CreateDirectory(OutputDirectory);

        var fileName = BuildFileName(notes.Title);
        var filePath = Path.Combine(OutputDirectory, fileName);
        var markdown = notes.MarkdownContent;

        await File.WriteAllTextAsync(filePath, markdown, Encoding.UTF8);
        return $"Notes saved to Markdown successfully: {filePath}";
    }

    private static string BuildFileName(string title)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(title
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray())
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "learning-notes";
        }

        return $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{sanitized}.md";
    }
}