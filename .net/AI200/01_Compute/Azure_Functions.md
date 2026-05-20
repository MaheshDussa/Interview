# Azure Functions (AZ-204)

> **One-liner**: Functions = **serverless** event-driven code — write a method, attach a **trigger**, Azure runs and scales it for you.

---

## 1. What is Azure Functions?

- **Serverless compute** — you don't manage servers, you write small functions.
- Each function has **one trigger** + zero-or-more **bindings** (inputs/outputs).
- Runs on **Functions Runtime** which sits on App Service infrastructure.

**Analogy**: A vending machine — you press a button (trigger), it dispenses (executes) only when needed. No "always running" cost.

---

## 2. Hosting Plans (memorize)

| Plan | Cold start | Scale | VNet | Use when |
|---|---|---|---|---|
| **Consumption** | Yes | Auto (event-driven) | Limited | Spiky, cheap, pay-per-execution |
| **Flex Consumption** | Low | Auto + always-ready instances | Yes | Modern default; pay-per-execution + VNet |
| **Premium (EP1/2/3)** | None | Pre-warmed | Yes | Low latency, VNet, no cold start |
| **Dedicated (App Service Plan)** | None | Manual/auto | Yes | Already paying for an ASP |
| **Container Apps hosted** | Low | KEDA-based | Yes | Containerized functions |

**Memory hint**: "Consumption is cheap but cold; Premium is warm but pricey; Dedicated is for piggybacking on an existing plan."

---

## 3. Triggers (one per function)

| Trigger | When it fires |
|---|---|
| **HTTP** | HTTP request hits endpoint |
| **Timer** | CRON schedule (`0 */5 * * * *` = every 5 min) |
| **Blob** | New/updated blob in container |
| **Queue** | Message in Storage Queue |
| **Service Bus** | Message in SB queue/topic |
| **Event Grid** | Event in any subscribed source |
| **Event Hub** | Telemetry/event stream |
| **Cosmos DB** | Change feed (insert/update) |
| **Durable** | Orchestration state change |

---

## 4. Bindings (inputs/outputs — declarative I/O)

```csharp
[Function("ProcessOrder")]
public async Task Run(
    [QueueTrigger("orders")] string message,              // trigger
    [BlobInput("data/config.json")] string configJson,    // input binding
    [CosmosDBOutput("db", "orders", Connection = "Cosmos")] IAsyncCollector<Order> outOrders) // output
{
    var order = JsonSerializer.Deserialize<Order>(message);
    await outOrders.AddAsync(order);
}
```

> Bindings remove boilerplate — no manual `BlobServiceClient`, `QueueClient`, etc.

---

## 5. C# Models — Isolated vs In-Process

| | In-process | **Isolated (default now)** |
|---|---|---|
| Runs as | Inside Functions host | Separate .NET process |
| .NET version | Tied to host | **Any** version (.NET 8/9) |
| Middleware | Limited | Full ASP.NET Core middleware |
| Future | Deprecated | Recommended |

**Exam tip**: Use **isolated worker** for new projects. In-process model is being phased out.

---

## 6. HTTP Trigger Example (Isolated)

```csharp
public class WeatherFn(ILogger<WeatherFn> log)
{
    [Function("GetWeather")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "weather/{city}")]
        HttpRequestData req, string city)
    {
        log.LogInformation("City: {City}", city);
        var res = req.CreateResponse(HttpStatusCode.OK);
        res.WriteString($"Sunny in {city}");
        return res;
    }
}
```

### Authorization levels
| Level | Who can call |
|---|---|
| `Anonymous` | Anyone |
| `Function` | Needs **function-specific** key |
| `Admin` | Needs **host master** key |
| `User` | Authenticated user (Easy Auth) |
| `System` | For Event Grid etc. |

---

## 7. Durable Functions (stateful workflows)

Three patterns:

| Pattern | Code |
|---|---|
| **Function Chaining** | `await ctx.CallActivityAsync("A");` then B then C |
| **Fan-out / Fan-in** | Parallel `Task.WhenAll(activities)` |
| **Async HTTP API** | Long-running job — client polls status URL |
| **Monitor** | Recurring check until condition met |
| **Human Interaction** | Wait for external event (approval) |

```csharp
[Function("OrderOrchestrator")]
public static async Task Run([OrchestrationTrigger] TaskOrchestrationContext ctx)
{
    var id = await ctx.CallActivityAsync<string>("CreateOrder", null);
    await ctx.CallActivityAsync("ChargeCard", id);
    await ctx.CallActivityAsync("ShipItem", id);
}
```

> Orchestrators must be **deterministic** — no `DateTime.Now`, no `Guid.NewGuid()`, no direct I/O.

---

## 8. local.settings.json (local only — NOT deployed)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "Cosmos": "AccountEndpoint=..."
  }
}
```

In Azure, these become **Application Settings** on the Function App.

---

## 9. host.json (deployed config)

```json
{
  "version": "2.0",
  "functionTimeout": "00:10:00",
  "extensions": {
    "http": { "routePrefix": "api" },
    "queues": { "maxPollingInterval": "00:00:02", "batchSize": 16 }
  },
  "logging": { "applicationInsights": { "samplingSettings": { "isEnabled": true } } }
}
```

**Timeout defaults**: Consumption = 5 min (max **10 min**); Premium/Dedicated = unlimited.

---

## 10. Scaling

- **Consumption / Flex** → scale controller monitors trigger → adds workers (1 per partition for Event Hub, etc.).
- **Per-function max instances**: `functionAppScaleLimit` (Premium up to 100, Flex configurable).
- **Always Ready instances** (Flex/Premium) → eliminate cold starts.

---

## 11. Deployment

```powershell
# From CLI
func azure functionapp publish myfuncapp

# ZIP
az functionapp deployment source config-zip -g rg1 -n myfuncapp --src bin/publish.zip

# Run from package (recommended)
az functionapp config appsettings set -g rg1 -n myfuncapp --settings WEBSITE_RUN_FROM_PACKAGE=1
```

---

## 12. Monitoring

- Wire **Application Insights** with `APPLICATIONINSIGHTS_CONNECTION_STRING`.
- View **invocations**, success rate, duration, exceptions in portal.
- Use **Live Metrics** for real-time.
- `func azure functionapp logstream myfuncapp` for tail.

---

## 13. Security

| Concern | Approach |
|---|---|
| Secrets | Key Vault references, **Managed Identity** |
| Identity to other services | System-assigned MI → grant RBAC |
| Public exposure | Use **APIM** in front, or **Private Endpoint** |
| Function keys | Rotate via portal / CLI |
| Auth on HTTP triggers | Use **Easy Auth** + `AuthorizationLevel.Anonymous` |

---

## 14. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Cold start latency | Use Premium / Flex with Always Ready |
| 10-min timeout exceeded | Use Durable Functions or Premium plan |
| Orchestrator non-deterministic bug | Don't call `DateTime.Now`, use `ctx.CurrentUtcDateTime` |
| Storage account deleted | Function app breaks — it needs `AzureWebJobsStorage` |
| Singleton state in static field | Don't — instances scale out |
| HTTP trigger returns 401 | Check function key / auth level |
| In-process model upgrade pain | Migrate to **isolated worker** |

---

## 15. AZ-204 Q&A

**Q1. Difference between trigger and binding?**
Trigger = how the function starts (exactly one). Binding = declarative input/output (zero or many).

**Q2. Consumption vs Premium plan?**
Consumption is pay-per-execution with cold starts. Premium has pre-warmed instances, VNet, longer timeout.

**Q3. How do Durable Functions store state?**
In **Azure Storage** (tables + queues) by default. State is rebuilt by replaying the orchestrator history — that's why it must be deterministic.

**Q4. What's `AzureWebJobsStorage`?**
The storage account the runtime uses for triggers, timers, leases, durable state. Mandatory.

**Q5. Max execution time?**
Consumption: 5 min default, 10 min max. Premium/Dedicated: unlimited (configurable).

**Q6. How does scaling work in Consumption?**
A separate **scale controller** watches trigger metrics (queue depth, event lag) and adds/removes workers.

**Q7. How to call a function securely from another Azure service?**
Use **Managed Identity** on the caller + **Easy Auth** on the function (require Entra ID).

**Q8. Function vs WebJob?**
WebJob runs inside an App Service; Function is its own resource with its own scaling model.

**Q9. Durable function patterns?**
Chaining, Fan-out/fan-in, Async HTTP, Monitor, Human interaction.

**Q10. What's `[FunctionName]` vs `[Function]`?**
In-process used `[FunctionName]`; **isolated** uses `[Function]`.

---

## 16. CLI Cheat Sheet

```powershell
# Create
az functionapp create -g rg1 -n myfuncapp \
  --storage-account mystg --consumption-plan-location eastus \
  --runtime dotnet-isolated --functions-version 4

# Settings
az functionapp config appsettings set -g rg1 -n myfuncapp --settings KEY=VAL

# Identity + KV access
az functionapp identity assign -g rg1 -n myfuncapp
az keyvault set-policy -n kv1 --object-id <mi-objectid> --secret-permissions get list

# Logs
az functionapp log tail -g rg1 -n myfuncapp
```

---

## 17. Mental Model

> **Function = code + trigger + bindings. Pick a plan based on cold-start + VNet needs. For multi-step workflows use Durable.**
