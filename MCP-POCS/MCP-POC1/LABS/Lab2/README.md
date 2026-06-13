# Lab2

## Title

Azure DevOps AI Project Assistant

## Objective

Build an MCP server that connects to Azure DevOps services and related engineering systems so developers can use Copilot to query work, delivery status, bugs, deployments, and release information through simple prompts.

## Connected Systems

- Azure DevOps Boards
- Azure DevOps Repos
- Azure DevOps Pull Requests
- Azure DevOps Pipelines
- Application Insights
- Azure DevOps Wiki

## Practical Use Cases

Developers can ask Copilot prompts like:

- `What tasks are assigned to me today?`
- `Create child tasks for User Story #123`
- `Why did yesterday's deployment fail?`
- `Show bugs related to Login module`
- `Create release notes from completed stories`

## MCP Tools

| Tool | Action |
| --- | --- |
| `get_my_work` | Get assigned work items |
| `create_task_breakdown` | Generate sub tasks |
| `sprint_summary` | Sprint progress |
| `release_notes` | Generate release notes |
| `deployment_health` | Check deployment status |
| `bug_analyzer` | Analyze recurring bugs |

## Implementation Status

All listed MCP tools have been added to the sidecar MCP server project:

- `get_my_work`
- `create_task_breakdown`
- `sprint_summary`
- `release_notes`
- `deployment_health`
- `bug_analyzer`

Implementation files:

- `MCP-POC1.McpServer/Lab2Tools.cs`
- `MCP-POC1.McpServer/AzureDevOpsMcpClient.cs`

## Required Environment Variables

Set these before running the Lab 2 tools:

- `AZDO_ORGANIZATION_URL`
- `AZDO_PROJECT`
- `AZDO_PAT`

Optional settings:

- `AZDO_TEAM`
- `AZDO_ASSIGNED_TO`
- `APPINSIGHTS_APP_ID`
- `APPINSIGHTS_API_KEY`

## Daily Value

This lab is intended to save developers and team leads around 30 to 60 minutes every day by reducing manual Azure DevOps navigation, status collection, and repetitive reporting tasks.

## Suggested Implementation Scope

### MCP Server Responsibilities

- Authenticate against Azure DevOps and telemetry sources
- Query work items assigned to the current user
- Analyze linked bugs, recent deployments, and sprint progress
- Generate structured summaries for release notes and standups
- Return concise responses that Copilot can present directly to the developer

### Suggested Backend Integrations

- Azure DevOps REST APIs
- Application Insights query APIs
- Wiki page retrieval for team documentation context

## Example Prompt Flow

1. User asks: `What tasks are assigned to me today?`
2. MCP tool `get_my_work` queries Azure DevOps Boards.
3. MCP server returns active and assigned work items.
4. Copilot summarizes them in natural language.

## Next Step

The next practical implementation step is to configure Azure DevOps credentials locally and validate each MCP tool against a real project.