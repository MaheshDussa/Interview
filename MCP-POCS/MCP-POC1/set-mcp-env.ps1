param(
    [ValidateSet("Process", "User", "Machine")]
    [string]$Scope = "User"
)

function Set-EnvVar {
    param(
        [string]$Name,
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        Write-Host "Skipping empty value for $Name"
        return
    }

    if ($Scope -eq "Process") {
        Set-Item -Path "Env:$Name" -Value $Value
    }
    else {
        [Environment]::SetEnvironmentVariable($Name, $Value, $Scope)
    }

    Write-Host "Set $Name"
}

Set-EnvVar "AZDO_ORGANIZATION_URL"       "https://dev.azure.com/your-org"
Set-EnvVar "AZDO_ORG_URL"                "https://dev.azure.com/your-org"
Set-EnvVar "AZDO_PROJECT"                "YourProject"
Set-EnvVar "AZDO_PAT"                    "your-azure-devops-pat"

Set-EnvVar "AZDO_TEAM"                   "Team1"
Set-EnvVar "AZDO_ASSIGNED_TO"            "your.name@company.com"
Set-EnvVar "AZDO_DEFAULT_AREA_PATH"      "YourProject"
Set-EnvVar "AZDO_DEFAULT_ITERATION_PATH" "YourProject\\Sprint 1"
Set-EnvVar "AZDO_DEFAULT_ASSIGNEE"       "your.name@company.com"
Set-EnvVar "AZDO_DEFAULT_TAGS"           "mcp-poc"
Set-EnvVar "AZDO_REPOSITORY"             "TaxCore.Api"
Set-EnvVar "AZDO_REVIEWER_IDENTITY"      "your.name@company.com"
Set-EnvVar "AZDO_RELEASE_DEFINITION_ID"  "1"

Set-EnvVar "SONARQUBE_URL"               "https://sonarqube.company.com"
Set-EnvVar "SONARQUBE_TOKEN"             "your-sonarqube-token"

Set-EnvVar "APPINSIGHTS_APP_ID"          "your-app-insights-app-id"
Set-EnvVar "APPINSIGHTS_API_KEY"         "your-app-insights-api-key"
Set-EnvVar "APPINSIGHTS_TENANT_ID"       "your-tenant-id"
Set-EnvVar "APPINSIGHTS_CLIENT_ID"       "your-client-id"
Set-EnvVar "APPINSIGHTS_CLIENT_SECRET"   "your-client-secret"

Set-EnvVar "ONENOTE_ACCESS_TOKEN"        "your-onenote-access-token"
Set-EnvVar "ONENOTE_SECTION_ID"          "your-onenote-section-id"
Set-EnvVar "LEARNING_NOTES_OUTPUT_DIR"   "C:\\Temp\\McpLearningNotes"

Write-Host ""
Write-Host "Done. Restart VS Code or restart the MCP server processes to pick up persisted variables."



##.\set-mcp-env.ps1 -Scope User