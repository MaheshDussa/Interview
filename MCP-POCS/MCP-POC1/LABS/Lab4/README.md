# Lab4

## Title

Code-to-Work-Item Traceability

## Objective

Give developers and leads a fast way to trace pull requests back to Azure Boards items, and to inspect which code artifacts are linked to a specific work item.

## MCP Tool

| Tool | Action |
| --- | --- |
| `code_to_work_item_traceability` | Shows linked work items for a PR, or linked code artifacts for a work item |

## Supported Scenarios

- `Show work items linked to PR 456`
- `Trace PR 456 in repo WebPortal to Azure Boards`
- `Show code artifacts linked to work item 1234`

## Supported Inputs

- `repositoryName`
- `pullRequestId`
- `workItemId`

## Behavior

When `pullRequestId` is provided, the tool:

- reads pull request metadata
- fetches linked Azure Boards work items
- summarizes the linked work item IDs, types, titles, and states

When `workItemId` is provided, the tool:

- reads the Azure Boards work item with relations expanded
- extracts linked code artifacts such as PR or commit references
- returns a traceability summary

## Required Configuration

- `AZDO_ORGANIZATION_URL`
- `AZDO_PROJECT`
- `AZDO_PAT`

Optional:

- `AZDO_REPOSITORY`

## Value

This lab reduces manual cross-checking between code changes and Azure Boards, which helps with audits, release confidence, and delivery tracking.