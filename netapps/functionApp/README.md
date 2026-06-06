# FunctionApp

Professional Azure Functions isolated worker solution targeting .NET 10 with an XML solution file (`.slnx`).

## Included trigger samples

- HTTP trigger
- Timer trigger
- Blob trigger
- Queue trigger
- Service Bus trigger
- Event Hubs trigger
- Event Grid trigger
- Cosmos DB trigger

## Structure

- `FunctionApp.slnx` - solution root
- `src/FunctionApp.Host` - Azure Functions host project

## Prerequisites

- .NET SDK 10.0.300 or later
- Azure Functions Core Tools v4 for local execution
- Azurite or an Azure Storage account for blob and queue triggers
- Azure Service Bus, Event Hubs, Event Grid, and Cosmos DB resources if you want to run those triggers end-to-end

## Local development

1. Copy `src/FunctionApp.Host/local.settings.sample.json` to `src/FunctionApp.Host/local.settings.json` if you want environment-specific values.
2. Fill in the connection strings and resource names.
3. Build the solution with `dotnet build FunctionApp.slnx`.
4. Run locally with Azure Functions Core Tools from `src/FunctionApp.Host` using `func start`.

## Notes

- This project uses the isolated worker model and Application Insights worker telemetry.
- `local.settings.json` is ignored by git; `local.settings.sample.json` is the checked-in template.

## Operations guide

- See `docs/azure-resources-and-usage.md` for the Azure resource checklist, required application settings, and trigger-by-trigger testing steps.