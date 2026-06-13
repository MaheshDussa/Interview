# Lab7

## Title

AI Learning Notes to Markdown

## Objective

Generate simple, memorable technical notes from a topic prompt and optionally save them to a local Markdown file for personal learning and interview revision.

## Example Prompt

```text
Create notes for Azure App Services in simple English, memorable format, with thumb rules, and save them to a Markdown file.
```

## MCP Tool

| Tool | Action |
| --- | --- |
| `create_learning_notes` | Generates topic notes in simple English and optionally saves them to Markdown |

## Output Format

The tool produces:

- a quick-notes table
- thumb rules for interviews and revision
- a short memory formula
- optional Markdown file creation

## Current Topic Templates

- Azure App Service
- IIS
- Azure DevOps Pipelines
- DP-800
- Generic fallback topic format

## Markdown Save Configuration

Optional environment variables:

- `LEARNING_NOTES_OUTPUT_DIR`
- `MARKDOWN_NOTES_OUTPUT_DIR`

If neither is set, files are written under `LearningNotes` in the MCP server working directory.

## Implementation Files

- `MCP-POC1.McpServer/Lab7Tools.cs`
- `MCP-POC1.McpServer/MarkdownNotesClient.cs`
- `MCP-POC1.McpServer/OneNoteNotesClient.cs`

## Value

This lab turns Copilot Chat into a personal AI learning notebook for Azure, .NET, Angular, IIS, SQL Server, and DevOps topics without requiring any external note service.