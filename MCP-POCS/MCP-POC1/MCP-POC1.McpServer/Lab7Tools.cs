using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP_POC1.McpServer;

[McpServerToolType]
public static class Lab7Tools
{
    [McpServerTool, Description("Creates simple-English learning notes in a memorable format with thumb rules and optionally saves them to a Markdown file.")]
    public static async Task<string> create_learning_notes(
        [Description("The learning topic, for example Azure App Service, IIS, or Azure DevOps Pipelines.")] string topic,
        [Description("Set to true to save the generated notes to a Markdown file. If not set, the tool returns the generated notes as text.")] bool saveToMarkdown = false)
    {
        var notes = OneNoteNotesClient.GenerateNotes(topic);
        var preview = OneNoteNotesClient.BuildPreviewText(notes);

        if (!saveToMarkdown)
        {
            return preview;
        }

        try
        {
            var client = MarkdownNotesClient.Create();
            var saveMessage = await client.SaveNotesAsync(notes);
            return string.Join(Environment.NewLine, preview, string.Empty, saveMessage);
        }
        catch (Exception exception)
        {
            return string.Join(Environment.NewLine, preview, string.Empty, $"The learning notes tool failed: {exception.Message}");
        }
    }
}
