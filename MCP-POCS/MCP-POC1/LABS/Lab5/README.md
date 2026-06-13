# Lab5

## Title

Sprint Health Dashboard AI

## Tech Stack

- Angular Frontend
- .NET API
- MCP Server
- Azure DevOps

## Objective

Provide an AI-assisted sprint dashboard that summarizes delivery health using Azure DevOps sprint data and exposes that summary through an MCP tool that a frontend or API can consume.

## Dashboard Signals

- Sprint completion %
- Delayed tasks
- Developer workload
- Risk prediction

## MCP Tool

| Tool | Action |
| --- | --- |
| `sprint_health_dashboard` | Returns a sprint health summary with completion, delayed tasks, workload distribution, and a simple risk level |

## Implementation Status

Lab 5 is implemented in the MCP server.

Implementation files:

- `MCP-POC1.McpServer/Lab5Tools.cs`
- `MCP-POC1.McpServer/AzureDevOpsMcpClient.cs`

## Tool Behavior

The tool:

- detects the current sprint for the selected team
- calculates completion percentage
- flags active items that have not changed within a threshold
- summarizes active workload by developer
- predicts sprint risk using a simple heuristic

## Inputs

- `team`
- `delayedAfterDays`
- `overloadedThreshold`

## Example Usage

```text
Use sprint_health_dashboard for the current team
```

```text
Use sprint_health_dashboard with delayedAfterDays 4 and overloadedThreshold 6
```

## Suggested Architecture

- Angular frontend calls the .NET API or MCP client layer
- .NET API can act as an orchestration layer for dashboard requests
- MCP server queries Azure DevOps and returns an AI-friendly sprint summary
- Azure DevOps remains the system of record for sprint state and assignments

## Required Configuration

- `AZDO_ORGANIZATION_URL`
- `AZDO_PROJECT`
- `AZDO_PAT`

Optional:

- `AZDO_TEAM`

## Value

This lab helps teams spot sprint risk earlier and reduces manual status gathering during standups and sprint reviews.