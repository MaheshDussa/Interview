# Application Resources Guide

This document lists the Azure resources and configuration needed to run this application end to end, including optional integrations that can be enabled without breaking the base API.

## 1. Resource overview

### Required for the base application

- Azure App Service or another .NET hosting target
- Azure SQL Database
- Azure Key Vault

### Required only if you want centralized monitoring

- Azure Application Insights

### Required only if you want file upload support

- Azure Storage Account with Blob Storage

### Required only if you want Azure AD protected API access

- Microsoft Entra ID app registration

### Required only for the separate practice module

- Event Grid custom topic or domain topic
- Event Hubs namespace and event hub
- Service Bus namespace and queue
- Azure Storage Account queue
- API Management instance

## 2. Recommended resource group layout

Use one resource group per environment.

Examples:

- `rg-task-api-dev`
- `rg-task-api-qa`
- `rg-task-api-stage`
- `rg-task-api-prod`

Recommended naming pattern by environment:

- App Service: `app-task-api-<env>`
- SQL Server: `sql-task-<env>`
- SQL Database: `sqldb-task-<env>`
- Key Vault: `kv-task-<env>`
- Application Insights: `appi-task-<env>`
- Storage Account: `sttask<env>`
- Service Bus: `sb-task-<env>`
- Event Hubs Namespace: `evhns-task-<env>`
- Event Grid Topic: `egt-task-<env>`
- API Management: `apim-task-<env>`

## 3. Minimum resource set to run the current application

### 3.1 Hosting

Create one of the following:

- Azure App Service for Windows or Linux
- Azure Container Apps if you later want to deploy from the Dockerfile
- IIS host if you are not deploying to Azure App Service

For Azure hosting, prefer assigning a managed identity to the app so it can read Key Vault without stored credentials.

### 3.2 Azure SQL Database

Needed because the API startup requires `ConnectionStrings:MyExpressConnection`.

Create:

- Azure SQL logical server
- Azure SQL database
- Firewall rule or private endpoint so the app can connect

Store the connection string in Key Vault as:

- `ConnectionStrings--MyExpressConnection`

### 3.3 Azure Key Vault

Needed to centralize configuration and secrets.

Create:

- One Key Vault per environment
- Access policy or RBAC assignment for the app's managed identity

Configure this application setting on the host:

- `KeyVault__VaultUri=https://<vault-name>.vault.azure.net/`

The app reads Key Vault early at startup, so once `KeyVault:VaultUri` is set, all other secrets can come from the vault.

### 3.4 Authentication mode

The application supports two modes:

- Local JWT mode
- Azure AD mode

#### Local JWT mode

Required if `AzureAd:ClientId` is not provided.

Secrets/configuration:

- `Jwt--Key`
- `Jwt--Issuer`
- `Jwt--Audience`

#### Azure AD mode

Required if you want bearer tokens issued by Microsoft Entra ID.

Create:

- App registration for the API
- Expose an API scope if client apps will request tokens

Configuration:

- `AzureAd--Instance`
- `AzureAd--TenantId`
- `AzureAd--ClientId`

Optional if used elsewhere later:

- `AzureAd--ClientSecret`

## 4. Optional application services

These integrations do not block startup when not configured.

### 4.1 Blob Storage for file uploads

Needed only if you will use `POST /api/files/upload`.

Create:

- Storage account
- Blob container

Secrets/configuration:

- `BlobStorage--ConnectionString`
- `BlobStorage--ContainerName`

Usage:

- Keep the feature off simply by leaving the settings empty.
- If the endpoint is called without configuration, it returns a service-unavailable response instead of breaking the app.

### 4.2 Application Insights

Needed only if you want telemetry, middleware request tracking, dependency telemetry enrichment, and unique authenticated user counts.

Create:

- Application Insights resource
- Log Analytics workspace if your org standard requires workspace-based monitoring

Secrets/configuration:

- `ApplicationInsights--ConnectionString`
- `ApplicationInsights--EnableAdaptiveSampling`

What gets captured:

- Request telemetry
- Custom request completion events from middleware
- Unique authenticated user identifiers
- Business events for login, task changes, uploads, and practice integrations

## 5. Separate Azure practice module resources

These are isolated under `/api/practice/azure` and disabled by default.

Enable the module with:

- `AzurePractice--Enabled=true`

### 5.1 Event Grid

Create:

- Event Grid custom topic

Secrets/configuration:

- `AzurePractice--EventGrid--Endpoint`
- `AzurePractice--EventGrid--AccessKey`

Endpoint:

- `POST /api/practice/azure/event-grid`

### 5.2 Event Hubs

Create:

- Event Hubs namespace
- Event hub

Secrets/configuration:

- `AzurePractice--EventHubs--ConnectionString`
- `AzurePractice--EventHubs--HubName`

Endpoint:

- `POST /api/practice/azure/event-hub`

### 5.3 Service Bus

Create:

- Service Bus namespace
- Queue

Secrets/configuration:

- `AzurePractice--ServiceBus--ConnectionString`
- `AzurePractice--ServiceBus--QueueName`

Producer endpoint:

- `POST /api/practice/azure/service-bus`

Optional consumer worker configuration:

- `AzurePractice--Consumers--ServiceBus--Enabled`
- `AzurePractice--Consumers--ServiceBus--MaxConcurrentCalls`

### 5.4 Queue Storage

Create:

- Storage account
- Queue

Secrets/configuration:

- `AzurePractice--QueueStorage--ConnectionString`
- `AzurePractice--QueueStorage--QueueName`

Producer endpoint:

- `POST /api/practice/azure/queue-storage`

Optional consumer worker configuration:

- `AzurePractice--Consumers--QueueStorage--Enabled`
- `AzurePractice--Consumers--QueueStorage--PollingIntervalSeconds`
- `AzurePractice--Consumers--QueueStorage--MaxMessagesPerPoll`
- `AzurePractice--Consumers--QueueStorage--VisibilityTimeoutSeconds`

## 6. API Management for practice and gateway control

API Management is not required for the API to run, but it is the correct Azure resource if you want gateway controls, products, policies, and external exposure patterns.

Create:

- One API Management instance per environment, or a shared non-prod instance for Dev/QA if your team accepts shared gateways

Typical use:

- Import the OpenAPI document from `/swagger/v1/swagger.json`
- Create products and subscription keys
- Add rate-limit policies
- Add JWT validation policies
- Inject headers, rewrite URLs, or add caching

See [docs/AzureIntegrationPractice.md](docs/AzureIntegrationPractice.md) for sample APIM steps and policy examples.

## 7. Environment files and configuration layering

The app supports these environment-specific files:

- `appsettings.Development.json`
- `appsettings.QA.json`
- `appsettings.Stage.json`
- `appsettings.Prod.json`

Environment aliases handled by the app:

- `Development` or `Dev`
- `QA`
- `Stage` or `Staging`
- `Prod` or `Production`

Recommended approach:

- Keep secrets out of JSON files
- Put only non-secret settings in environment-specific JSON files
- Put secrets in Key Vault
- Set `ASPNETCORE_ENVIRONMENT` per environment

## 8. Key Vault secret inventory

### Base application

- `ConnectionStrings--MyExpressConnection`
- `Jwt--Key`
- `Jwt--Issuer`
- `Jwt--Audience`
- `AzureAd--ClientId`
- `AzureAd--TenantId`
- `AzureAd--Instance`
- `AzureAd--ClientSecret`

### Optional application features

- `BlobStorage--ConnectionString`
- `BlobStorage--ContainerName`
- `ApplicationInsights--ConnectionString`

### Practice module

- `AzurePractice--Enabled`
- `AzurePractice--EventGrid--Endpoint`
- `AzurePractice--EventGrid--AccessKey`
- `AzurePractice--EventHubs--ConnectionString`
- `AzurePractice--EventHubs--HubName`
- `AzurePractice--ServiceBus--ConnectionString`
- `AzurePractice--ServiceBus--QueueName`
- `AzurePractice--QueueStorage--ConnectionString`
- `AzurePractice--QueueStorage--QueueName`

### Practice consumers

- `AzurePractice--Consumers--ServiceBus--Enabled`
- `AzurePractice--Consumers--ServiceBus--MaxConcurrentCalls`
- `AzurePractice--Consumers--QueueStorage--Enabled`
- `AzurePractice--Consumers--QueueStorage--PollingIntervalSeconds`
- `AzurePractice--Consumers--QueueStorage--MaxMessagesPerPoll`
- `AzurePractice--Consumers--QueueStorage--VisibilityTimeoutSeconds`

## 9. Recommended rollout order

### For the base API

1. Create resource group.
2. Create App Service or hosting target.
3. Create Azure SQL server and database.
4. Create Key Vault.
5. Grant the app access to Key Vault.
6. Add the SQL and auth secrets to Key Vault.
7. Set `KeyVault__VaultUri` on the app host.
8. Deploy the API.
9. Verify login and task endpoints.

### For optional features

1. Add Application Insights and its connection string.
2. Add Blob Storage and upload settings.
3. Add Azure AD settings if switching from local JWT to Entra ID.

### For practice services

1. Create one messaging resource at a time.
2. Add only that provider's settings.
3. Set `AzurePractice--Enabled=true`.
4. Test the matching `/api/practice/azure/...` endpoint.
5. Enable the matching background consumer only if you want to practice receive flows.
6. Import the API into APIM and test policies there.

## 10. How to operate without breaking the app

Use this rule:

- Treat SQL and base auth as mandatory.
- Treat Blob Storage, App Insights, APIM, Event Grid, Event Hubs, Service Bus, Queue Storage, and practice consumers as optional.

That means:

- Missing SQL or required auth config should block startup because the app cannot function correctly.
- Missing optional integration config should not block startup.
- Optional endpoints and hosted workers should fail gracefully or remain inactive until configured.

## 11. Quick checklist by environment

For each of Dev, QA, Stage, and Prod verify:

- Resource group exists
- Hosting target exists
- SQL server and database exist
- Key Vault exists
- Managed identity or secret access to Key Vault is configured
- `KeyVault__VaultUri` is set on the app
- Required DB and auth secrets exist in Key Vault
- Environment name is set correctly
- Optional integration secrets are present only for features you intend to use

## 12. References in this repo

- Practice integration details: [docs/AzureIntegrationPractice.md](docs/AzureIntegrationPractice.md)
- Sample requests: [WebApplication1.http](WebApplication1.http)
- Configuration template: [appsettings.json](appsettings.json)