# Labs

This repository keeps lab documentation under the `LABS` folder.

## Environment Setup

The MCP servers now load environment variables from a root `.env` file when they start. Existing process or user environment variables still take precedence over values in `.env`.

Typical workflow:

- update `.env` in the workspace root with your local values
- restart VS Code or restart the MCP server process
- use `set-mcp-env.ps1` only if you want to persist values outside the workspace

## MCP Servers

- `MCP-POC1.McpServer`: Azure DevOps-focused MCP tools (Labs 2-6)
- `MCP-POC1.Learning.McpServer`: greeting and learning-notes MCP tools
- `MCP-POC1.AIQuality.McpServer`: SonarQube, App Insights, repository intelligence, remediation, and PR quality tools

## AI Quality MCP Server

The AI quality server exposes a first implementation slice for the architecture and SRE use case.

Available tools:

- `sonarqube_quality_overview`
- `app_insights_failure_summary`
- `repository_intelligence`
- `root_cause_analysis`
- `create_remediation_work_items`
- `root_cause_to_remediation_work_items`
- `pr_review_assistant`

The remediation planner now generates a richer Azure Boards hierarchy using fallback work item types where needed:

- `Epic` or fallback parent
- `Feature` or fallback parent
- `User Story` or `Product Backlog Item`
- `Task` items for investigation, implementation, security hardening when relevant, and validation

Environment variables:

- Azure DevOps: `AZDO_ORGANIZATION_URL`, `AZDO_PROJECT`, `AZDO_PAT`
- Optional Azure DevOps defaults: `AZDO_REPOSITORY`, `AZDO_DEFAULT_AREA_PATH`, `AZDO_DEFAULT_ITERATION_PATH`, `AZDO_DEFAULT_ASSIGNEE`, `AZDO_DEFAULT_TAGS`
- SonarQube: `SONARQUBE_URL`, `SONARQUBE_TOKEN`
- Application Insights: `APPINSIGHTS_APP_ID` plus either `APPINSIGHTS_API_KEY` or Azure AD credentials (`APPINSIGHTS_TENANT_ID`, `APPINSIGHTS_CLIENT_ID`, `APPINSIGHTS_CLIENT_SECRET`). Azure AD is the preferred option because API keys are being retired.

Example prompts:

- `Analyze SonarQube findings for project taxcore-api`
- `Show the last 7 days of App Insights failures`
- `Why is production failing for project taxcore-api?`
- `Run root cause to remediation workflow for project taxcore-api and create Azure Boards items`
- `Review pull request 123 in repository TaxCore.Api`

## Available Labs

- `Lab1`: `LABS/Lab1/README.md`
- `Lab2`: `LABS/Lab2/README.md`
- `Lab3`: `LABS/Lab3/README.md`
- `Lab4`: `LABS/Lab4/README.md`
- `Lab5`: `LABS/Lab5/README.md`
- `Lab6`: `LABS/Lab6/README.md`
- `Lab7`: `LABS/Lab7/README.md`