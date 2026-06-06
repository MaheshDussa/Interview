# GitHub Workflow Notes

This folder contains the GitHub Actions workflows for the API, Angular app, and database project.

## Workflow Overview

| Workflow | Purpose | Trigger |
| --- | --- | --- |
| `workflows/dotnet-ci.yml` | Build, test, and package the .NET API | Push, pull request, manual |
| `workflows/dotnet-cd.yml` | Deploy the .NET API to Azure App Service | Manual |
| `workflows/angular-ci.yml` | Build, test, and package the Angular app | Push, pull request, manual |
| `workflows/angular-cd.yml` | Deploy the Angular app to Azure App Service | Manual |
| `workflows/db-ci.yml` | Build and package the SSDT database project as a DACPAC | Push, pull request, manual |
| `workflows/db-cd.yml` | Publish the DACPAC to Azure SQL Database | Manual |

## `dotnet-ci.yml`

Purpose: validates the .NET API in `netapps/WebApplication1` and publishes a build artifact.

Key points:
- Runs on `ubuntu-latest`.
- Restores, builds, runs tests, and packages the publish output.
- Packages the publish output as `artifacts/webapplication1-release.tar.gz`.
- Uploads test results and published artifacts.

Notes:
- Triggered only when the API project or the workflow file changes.
- The test step is configured with `continue-on-error: true`, so it reports issues without failing the whole job.

## `dotnet-cd.yml`

Purpose: deploys the .NET API to Azure App Service.

Key points:
- Uses `workflow_dispatch` with `staging` or `production`.
- Requires the `artifact_run_id` input from a successful `.NET` CI run.
- Downloads the `dotnet-artifacts` package from CI and deploys it without rebuilding.
- Deploys with `azure/webapps-deploy@v3`.
- Includes a basic post-deployment health check.

Required configuration:
- GitHub secret: `AZURE_CREDENTIALS`.
- Workflow env values: `AZURE_RESOURCE_GROUP`, `AZURE_STAGING_APP_SERVICE`, `AZURE_PRODUCTION_APP_SERVICE`.

Notes:
- The current file already contains concrete Azure values. Review them before using the workflow in another environment.

## `angular-ci.yml`

Purpose: validates and packages the Angular application in `feapps/TaskMgmt-1`.

Key points:
- Runs on `ubuntu-latest` with Node `18.x`.
- Installs dependencies with `npm ci`.
- Runs tests in non-blocking mode.
- Builds the production bundle and uploads a deployable `angular-app.zip` artifact.

Notes:
- Triggered only when the Angular app or the workflow file changes.
- Build output is expected under `dist/task-mgmt`.

## `angular-cd.yml`

Purpose: deploys the Angular app to Azure App Service.

Key points:
- Uses `workflow_dispatch` with `staging` or `production`.
- Requires the `artifact_run_id` input from a successful Angular CI run.
- Downloads `angular-app.zip` from CI and deploys it without rebuilding.
- Deploys with `azure/webapps-deploy@v3`.

Required configuration:
- GitHub secret: `AZURE_CREDENTIALS`.
- Workflow env values: `AZURE_RESOURCE_GROUP`, `AZURE_STAGING_APP_SERVICE`, `AZURE_PRODUCTION_APP_SERVICE`.

Notes:
- This workflow packages the browser output because the app is built with Angular SSR and the browser bundle is the deployable web asset set for the current App Service flow.

## `db-ci.yml`

Purpose: builds the SQL Server database project in `dsa/dblearning` and publishes the DACPAC artifact.

Key points:
- Runs on `windows-latest` because the project is an SSDT `.sqlproj`.
- Builds `dsa/dblearning/dblearning.sqlproj` with MSBuild.
- Verifies that `dsa/dblearning/bin/Release/dblearning.dacpac` exists.
- Packages the DACPAC as `artifacts/database-build.zip`.

Notes:
- Triggered only when the database project or the workflow file changes.
- The workflow intentionally validates only the DACPAC, because the SSDT build on GitHub Actions does not reliably emit a standalone `.sql` deployment script in `bin/Release`.

## `db-cd.yml`

Purpose: publishes the built DACPAC to Azure SQL Database.

Key points:
- Uses `workflow_dispatch` with `staging` or `production`.
- Runs on `windows-latest`.
- Requires the `artifact_run_id` input from a successful database CI run.
- Installs `microsoft.sqlpackage` as a .NET global tool.
- Downloads `database-build.zip`, extracts the DACPAC, and publishes it with `sqlpackage`.

Required configuration:
- GitHub secret: `AZURE_CREDENTIALS`.
- GitHub environment secret: `AZURE_SQL_ADMIN_PASSWORD`.
- Workflow env values: `AZURE_SQL_SERVER_FQDN`, `AZURE_SQL_ADMIN_USERNAME`, `AZURE_STAGING_DATABASE_NAME`, `AZURE_PRODUCTION_DATABASE_NAME`.

Operational notes:
- The GitHub runner must be allowed to reach Azure SQL. If public network access is restricted, use firewall rules or a private/self-hosted runner.
- The publish command uses `/p:BlockOnPossibleDataLoss=false` and `/p:DropObjectsNotInSource=false`. Review those settings before production use.

## Setup Checklist

Before running the CD workflows:

1. Replace placeholder Azure resource values in the deployment workflows.
2. Run the matching CI workflow and note the CI run ID for the artifact you want to deploy.
3. Add the required GitHub secrets.
4. If you use GitHub Environments, add environment approvals and environment-scoped secrets for `staging` and `production`.
5. Confirm that Azure App Service and Azure SQL networking rules allow deployment from GitHub Actions.