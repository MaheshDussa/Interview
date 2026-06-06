# Azure Practice Module

This project now includes a separate practice surface under `/api/practice/azure` for Azure Event Grid, Event Hubs, Service Bus, and Queue Storage.

## Runtime isolation

- Practice endpoints are separate from the existing task APIs.
- They are enabled only when `AzurePractice:Enabled` is `true`.
- Each provider is optional and only fails when its own endpoint is called.

## Configuration keys

Use Azure Key Vault or environment variables for these keys:

- `AzurePractice--Enabled`
- `AzurePractice--EventGrid--Endpoint`
- `AzurePractice--EventGrid--AccessKey`
- `AzurePractice--EventHubs--ConnectionString`
- `AzurePractice--EventHubs--HubName`
- `AzurePractice--ServiceBus--ConnectionString`
- `AzurePractice--ServiceBus--QueueName`
- `AzurePractice--QueueStorage--ConnectionString`
- `AzurePractice--QueueStorage--QueueName`
- `AzurePractice--Consumers--ServiceBus--Enabled`
- `AzurePractice--Consumers--ServiceBus--MaxConcurrentCalls`
- `AzurePractice--Consumers--QueueStorage--Enabled`
- `AzurePractice--Consumers--QueueStorage--PollingIntervalSeconds`
- `AzurePractice--Consumers--QueueStorage--MaxMessagesPerPoll`
- `AzurePractice--Consumers--QueueStorage--VisibilityTimeoutSeconds`

## API Management practice

Create an API Management instance with Azure CLI:

```powershell
az apim create \
  --name <apim-name> \
  --resource-group <resource-group> \
  --location <location> \
  --publisher-email <email> \
  --publisher-name <publisher-name> \
  --sku-name Consumption
```

Import this API from the app's OpenAPI endpoint:

```powershell
az apim api import \
  --resource-group <resource-group> \
  --service-name <apim-name> \
  --path task-api \
  --api-id task-api \
  --specification-url https://<app-host>/swagger/v1/swagger.json \
  --specification-format OpenApiJson
```

Recommended APIM exercises:

- Create a product and require subscription keys.
- Add a rate limit policy to the practice endpoints.
- Add a JWT validation policy for the protected APIs.
- Rewrite backend URLs or strip headers in inbound policy.
- Add response caching for safe GET endpoints only.

Sample APIM policy:

```xml
<policies>
  <inbound>
    <base />
    <rate-limit calls="10" renewal-period="60" />
    <set-header name="x-apim-practice" exists-action="override">
      <value>enabled</value>
    </set-header>
  </inbound>
  <backend>
    <base />
  </backend>
  <outbound>
    <base />
  </outbound>
  <on-error>
    <base />
  </on-error>
</policies>
```

## Practice flows

- Event Grid: publish domain-style events.
- Event Hubs: send telemetry-style stream messages.
- Service Bus: send business queue messages.
- Queue Storage: enqueue simple background work items.
- Service Bus consumer: optional hosted worker consumes from the practice queue.
- Queue Storage consumer: optional hosted worker polls and deletes processed practice messages.

Use [WebApplication1.http](WebApplication1.http) for sample requests after obtaining a JWT token.