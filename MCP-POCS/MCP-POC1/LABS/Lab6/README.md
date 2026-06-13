# Lab6

## Title

Developer Daily Assistant

## Objective

Give developers a single Copilot command that summarizes what needs attention today instead of checking multiple Azure DevOps portals manually.

## Example Prompt

```text
What should I work on today?
```

## Returned Summary

- Assigned stories and tasks
- Blocked tasks
- Pending PR reviews
- Failed builds
- Upcoming releases

## MCP Tool

| Tool | Action |
| --- | --- |
| `developer_daily_assistant` | Produces one daily summary across work items, PR reviews, builds, and releases |

## Implementation Status

Lab 6 is implemented in the MCP server.

Implementation files:

- `MCP-POC1.McpServer/Lab6Tools.cs`
- `MCP-POC1.McpServer/AzureDevOpsMcpClient.cs`

## Inputs

- `assignedTo`
- `repositoryName`
- `daysBack`

## Tool Behavior

The tool:

- reads active assigned work items
- flags blocked items using state or tags
- checks active pull requests for pending reviews for the selected developer
- lists recent failed builds
- shows upcoming active releases when available

## Required Configuration

- `AZDO_ORGANIZATION_URL`
- `AZDO_PROJECT`
- `AZDO_PAT`

Optional:

- `AZDO_REPOSITORY`
- `AZDO_ASSIGNED_TO`
- `AZDO_DEFAULT_ASSIGNEE`

## Value

This lab replaces several manual status checks with one command inside Copilot Chat.