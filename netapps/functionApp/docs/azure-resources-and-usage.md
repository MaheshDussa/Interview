# Azure Resources And Usage Guide

This document describes which Azure resources are required for this function app and how to configure them so every trigger in `FunctionApp.Host` can run successfully.

## Solution scope

The function app currently contains these triggers:

- HTTP: `HttpHealthFunction`
- Timer: `TimerHeartbeatFunction`
- Blob Storage: `BlobIngestFunction`
- Queue Storage: `QueueMessageFunction`
- Service Bus: `ServiceBusMessageFunction`
- Event Hubs: `EventHubMessageFunction`
- Event Grid: `EventGridNotificationFunction`
- Cosmos DB: `CosmosChangeFeedFunction`

## Azure resources to create

Create these resources in the same subscription and region unless you have a reason to split them:

| Resource | Required | Purpose | Used by |
| --- | --- | --- | --- |
| Azure Function App | Yes | Hosts the .NET 10 isolated worker application | All functions |
| Storage Account | Yes | Function host storage, blob trigger, and queue trigger | Host, Blob, Queue |
| Application Insights | Recommended | Telemetry and diagnostics | All functions |
| Service Bus namespace with queue `sample-queue` | Required for Service Bus trigger | Receives Service Bus messages | `ServiceBusMessageFunction` |
| Event Hubs namespace with hub `sample-hub` | Required for Event Hubs trigger | Receives streamed events | `EventHubMessageFunction` |
| Event Grid topic or system topic subscription | Required for Event Grid trigger | Sends events into the function | `EventGridNotificationFunction` |
| Cosmos DB account with database `app-db` and container `items` | Required for Cosmos DB trigger | Provides change feed input | `CosmosChangeFeedFunction` |
| Cosmos DB lease container `leases` | Required for Cosmos DB trigger | Stores change feed leases | `CosmosChangeFeedFunction` |

## Resource details

### 1. Function App

Create an Azure Function App configured for the isolated worker model.

Recommended settings:

- Runtime stack: `.NET Isolated`
- Functions version: `4`
- Hosting plan: Consumption, Flex Consumption, or Premium depending on workload
- OS: Windows or Linux

Deploy this project from the repository root after building.

### 2. Storage Account

This project uses `AzureWebJobsStorage` for two separate responsibilities:

- Azure Functions runtime state and host coordination
- Blob trigger container `samples-workitems`
- Queue trigger queue `sample-queue`

Create the following storage artifacts:

- Blob container: `samples-workitems`
- Queue: `sample-queue`

For local development you can use Azurite with `UseDevelopmentStorage=true`.

### 3. Application Insights

Create an Application Insights resource and copy its connection string into:

- `APPLICATIONINSIGHTS_CONNECTION_STRING`

This is not required for the code to compile, but it is the correct production telemetry setup for this app.

### 4. Service Bus

Create:

- A Service Bus namespace
- A queue named `sample-queue`

Copy a connection string with listen rights into:

- `ServiceBusConnection`

### 5. Event Hubs

Create:

- An Event Hubs namespace
- An event hub named `sample-hub`
- A consumer group if your environment needs one beyond the default

Copy the namespace connection string into:

- `EventHubConnection`

Set the hub name into:

- `SampleEventHubName`

### 6. Event Grid

Create one of these depending on your publisher:

- A custom Event Grid topic
- A system topic with an event subscription

Then subscribe it to the deployed function app's Event Grid trigger endpoint through the Azure portal. This is the safest setup because Azure handles the webhook validation flow for you.

### 7. Cosmos DB

Create:

- A Cosmos DB account for NoSQL
- A database named `app-db`
- A container named `items`
- A container named `leases`

Copy the Cosmos DB connection string into:

- `CosmosDbConnection`

The function is configured with `CreateLeaseContainerIfNotExists = true`, but pre-creating `leases` is still cleaner for controlled environments.

## Application settings mapping

These settings must exist in the Azure Function App configuration or in local settings when running locally:

| Setting name | Required for | Example |
| --- | --- | --- |
| `AzureWebJobsStorage` | Function runtime, Blob trigger, Queue trigger | `DefaultEndpointsProtocol=https;AccountName=...` |
| `FUNCTIONS_WORKER_RUNTIME` | Function runtime | `dotnet-isolated` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Telemetry | `InstrumentationKey=...` or full connection string |
| `ServiceBusConnection` | Service Bus trigger | `Endpoint=sb://...` |
| `EventHubConnection` | Event Hubs trigger | `Endpoint=sb://...` |
| `CosmosDbConnection` | Cosmos DB trigger | `AccountEndpoint=https://...;AccountKey=...;` |
| `SampleEventHubName` | Event Hubs trigger | `sample-hub` |

## Local setup

1. Install .NET SDK `10.0.300` or later.
2. Install Azure Functions Core Tools v4.
3. Start Azurite if you want to exercise Blob and Queue triggers locally.
4. Copy `src/FunctionApp.Host/local.settings.sample.json` to `src/FunctionApp.Host/local.settings.json`.
5. Replace placeholder connection strings with real values.
6. Build the solution with `dotnet build FunctionApp.slnx`.
7. Run the function app from `src/FunctionApp.Host` with `func start`.

## How to test each trigger

### HTTP trigger

- Function: `HttpHealthFunction`
- Route: `GET /api/health`
- Use the function key because the authorization level is `Function`

Example:

```powershell
curl "http://localhost:7071/api/health?code=<function-key>"
```

Expected result: HTTP 200 with a timestamp message.

### Timer trigger

- Function: `TimerHeartbeatFunction`
- Schedule: every 5 minutes

How to verify:

- Start the host and watch logs.
- Confirm a heartbeat log appears every five minutes.

### Blob trigger

- Function: `BlobIngestFunction`
- Container: `samples-workitems`

How to test:

1. Upload a text file into `samples-workitems`.
2. Check the function logs.

Expected result: the function logs the blob name and content length.

### Queue trigger

- Function: `QueueMessageFunction`
- Queue: `sample-queue` in the storage account from `AzureWebJobsStorage`

How to test:

1. Add a message to the `sample-queue` queue.
2. Check the function logs.

Expected result: the function logs the queue message.

### Service Bus trigger

- Function: `ServiceBusMessageFunction`
- Queue: `sample-queue`

How to test:

1. Send a message into the Service Bus queue `sample-queue`.
2. Check the function logs.

Expected result: the function logs the Service Bus message.

### Event Hubs trigger

- Function: `EventHubMessageFunction`
- Event hub: value from `SampleEventHubName`, default `sample-hub`

How to test:

1. Publish one or more events to the configured event hub.
2. Check the function logs.

Expected result: the function logs the number of messages received in the batch.

### Event Grid trigger

- Function: `EventGridNotificationFunction`

How to test:

1. Deploy the function app.
2. Create an Event Grid subscription that targets this function.
3. Publish an event from the topic or source resource.
4. Check the function logs.

Expected result: the function logs the incoming Event Grid payload.

### Cosmos DB trigger

- Function: `CosmosChangeFeedFunction`
- Database: `app-db`
- Monitored container: `items`
- Lease container: `leases`

How to test:

1. Insert or update documents in the `items` container.
2. Check the function logs.

Expected result: the function logs the number of changed documents.

## Deployment checklist

Before you deploy, verify these items:

- The Function App exists and targets Azure Functions v4.
- All application settings are present in the Function App configuration.
- The storage account contains the required blob container and queue.
- The Service Bus queue `sample-queue` exists.
- The Event Hub `sample-hub` exists.
- The Cosmos DB database and containers exist.
- Event Grid subscriptions are pointed at the deployed function.
- Application Insights is connected.

## Recommended order of work

Use this order to avoid chasing configuration failures across multiple services:

1. Get HTTP and Timer working first.
2. Configure Storage and validate Blob and Queue triggers.
3. Configure Service Bus and Event Hubs.
4. Configure Cosmos DB.
5. Configure Event Grid after the app is deployed and publicly reachable.

## Current limitations in this workspace

- Azure Functions Core Tools is not installed in this environment, so local execution was not verified here.
- The project builds successfully with `dotnet build FunctionApp.slnx`.