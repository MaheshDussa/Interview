# Lab3

## Title

Smart Work Item Creator

## Objective

Reduce the time teams spend manually creating Azure Boards items by using an MCP tool that turns a simple feature prompt into a linked work-item bundle.

## Example Prompt

```text
Implement Forgot Password feature
```

## Expected Outcome

The MCP tool can automatically prepare or create:

- Backend task
- Angular UI task
- API task
- Unit testing task
- Deployment task

It also links the generated tasks together in Azure Boards under a parent work item.

## MCP Tool

| Tool | Action |
| --- | --- |
| `smart_work_item_creator` | Creates or previews a parent feature item and linked child implementation tasks |

## Implementation Status

Lab 3 is implemented in the MCP server.

Implementation files:

- `MCP-POC1.McpServer/Lab3Tools.cs`
- `MCP-POC1.McpServer/AzureDevOpsMcpClient.cs`

## Tool Behavior

The `smart_work_item_creator` tool supports two modes:

- Preview mode: returns the parent item type and suggested child tasks without creating anything
- Create mode: creates a parent work item and linked child tasks in Azure Boards
- Template mode: changes the generated bundle based on `featureType`
- Auto-field mode: applies tags, area path, iteration path, and assignee automatically when supplied or configured by default

## Supported Inputs

- `featureTitle`
- `featureDescription`
- `createInAzureDevOps`
- `parentWorkItemId`
- `uiTechnology`
- `parentWorkItemType`
- `featureType`
- `tags`
- `areaPath`
- `iterationPath`
- `assignee`

## Supported Templates

- `fullstack`
- `api`
- `ui`
- `integration`
- `data`
- `security`
- `bugfix`

## Generated Task Bundles

For a `fullstack` feature request, the tool generates:

- Backend task
- UI task using the selected UI technology, defaulting to Angular
- API task
- Unit testing task
- Deployment task

Other templates generate a different task bundle shaped for API-only, UI-only, integration, data, security, or bug-fix work.

## Required Configuration

Set these environment variables before using create mode:

- `AZDO_ORGANIZATION_URL`
- `AZDO_PROJECT`
- `AZDO_PAT`

Optional defaults:

- `AZDO_DEFAULT_AREA_PATH`
- `AZDO_DEFAULT_ITERATION_PATH`
- `AZDO_DEFAULT_ASSIGNEE`
- `AZDO_DEFAULT_TAGS`

## Example Usage

Preview only:

```text
Use smart_work_item_creator for "Implement Forgot Password feature"
```

Create linked items:

```text
Use smart_work_item_creator to create Azure Boards items for "Implement Forgot Password feature"
```

## Value

This lab removes repetitive manual task creation and enforces a consistent implementation checklist for new features.