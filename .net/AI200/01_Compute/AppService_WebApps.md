# Azure App Service — Web Apps (AZ-204)

> **One-liner**: App Service is a fully managed **PaaS** that runs your web app/API on a hidden VM ("App Service Plan") so you only worry about code, not the OS, patches, or IIS.

---

## 1. What is App Service?

- A managed hosting platform for **web apps, REST APIs, mobile backends**.
- Supports **.NET, Java, Node, Python, PHP, Ruby**, and **custom containers**.
- Built-in: load balancing, auto-scaling, SSL, custom domains, deployment slots, auth, diagnostics.
- Runs on Windows or Linux workers managed by Microsoft.

**Analogy**: Renting a furnished apartment — you bring your stuff (code), the landlord (Azure) handles plumbing, electricity, security.

---

## 2. App Service Plan (ASP) vs App Service

| Concept | What it is |
|---|---|
| **App Service Plan** | The VM(s) + pricing tier (CPU/RAM/features). You pay for this. |
| **Web App** | The actual app deployed onto the plan. Free. |

> Multiple Web Apps can share **one** App Service Plan (they share CPU/RAM).

---

## 3. Pricing Tiers (memorize for exam)

| Tier | Code | Use case | Key features |
|---|---|---|---|
| Free | F1 | Demo only | 60 min/day CPU, no custom domain |
| Shared | D1 | Dev only | 240 min/day CPU |
| **Basic** | B1/B2/B3 | Dev/test | Custom domain, manual scale, **no slots, no autoscale** |
| **Standard** | S1/S2/S3 | Production | **5 slots**, auto-scale, daily backup |
| **Premium v3** | P1v3/P2v3/P3v3 | Production high-perf | **20 slots**, faster CPU, zone redundancy |
| **Isolated v2** | I1v2+ | Regulated/compliance | Runs in **App Service Environment (ASE)** inside a VNet |

**Memory hint**: F-D-B-S-P-I = "Free Dev Basic Standard Premium Isolated".

---

## 4. Deployment Options

| Method | When to use |
|---|---|
| Visual Studio Publish | Local dev |
| `az webapp up` | Quick CLI deploy |
| ZIP deploy (`az webapp deploy --src-path`) | CI/CD artifact |
| GitHub Actions / Azure DevOps | Production CI/CD |
| Docker Hub / ACR | Container-based apps |
| Local Git / FTP | Legacy |
| **Run from package** (`WEBSITE_RUN_FROM_PACKAGE=1`) | Atomic, read-only deploy |

```powershell
# Quick deploy current folder
az webapp up --name myapp --resource-group rg1 --runtime "DOTNET:9.0"

# ZIP deploy
az webapp deploy --resource-group rg1 --name myapp --src-path .\publish.zip --type zip
```

---

## 5. Deployment Slots (Standard+ only)

- Slots are **separate live web apps** that share the same plan.
- Common slots: `production`, `staging`, `qa`.
- **Swap** with zero downtime — and **auto-warm-up** before traffic shifts.
- Slot-specific settings (sticky): mark app settings as **"Deployment slot setting"** so they stay on the slot when swapped.

```powershell
az webapp deployment slot create -n myapp -g rg1 --slot staging
az webapp deployment slot swap   -n myapp -g rg1 --slot staging --target-slot production
```

> **Exam trap**: Swapping does **NOT** copy slot-sticky settings. Connection strings/app settings marked as sticky stay put.

---

## 6. App Settings & Connection Strings

- Surfaced to the app as **environment variables** (`Environment.GetEnvironmentVariable`).
- In ASP.NET Core they map to `IConfiguration` automatically.
- Connection strings appear as `SQLAZURECONNSTR_<name>`, `CUSTOMCONNSTR_<name>`, etc.
- Use **Key Vault references**: `@Microsoft.KeyVault(SecretUri=...)` to avoid storing secrets.

```
ConnectionStrings:Db = @Microsoft.KeyVault(SecretUri=https://kv1.vault.azure.net/secrets/db/abc)
```

---

## 7. Scaling

| Type | What it does |
|---|---|
| **Scale Up** | Change the SKU (bigger VM) — vertical |
| **Scale Out** | Add more instances — horizontal (Standard+) |
| **Autoscale** | Rules based on CPU/Memory/Queue length/Schedule |

```powershell
az monitor autoscale create -g rg1 --resource myapp --resource-type Microsoft.Web/sites \
  --name autoscale1 --min-count 2 --max-count 10 --count 2
```

> Free/Shared/Basic = manual scale only. Standard+ supports autoscale.

---

## 8. Authentication / Authorization ("Easy Auth")

- Built-in middleware — **no code required**.
- Providers: **Microsoft Entra ID, Google, Facebook, X, Apple, OpenID Connect**.
- Sits as a module **in front** of your app: rejects unauth requests or passes claims via `X-MS-CLIENT-PRINCIPAL` header.
- Configure: **App Service → Authentication → Add identity provider**.

> Use Easy Auth when you don't want to write OIDC code. For fine-grained policy, do it in your app with `Microsoft.Identity.Web`.

---

## 9. Custom Domains & TLS

1. Add CNAME (`www`) or A record (apex) at your DNS provider.
2. App Service → **Custom domains** → Add → Validate.
3. TLS options:
   - **App Service Managed Certificate** (free, auto-renew, no wildcard).
   - **Bring your own** PFX.
   - **Key Vault certificate**.
4. Enforce **HTTPS Only = On**, **Min TLS = 1.2**.

---

## 10. Networking

| Feature | Purpose |
|---|---|
| **VNet Integration** | Outbound from app → resources in a VNet (DB, Redis) |
| **Private Endpoint** | Inbound — app reachable only via private IP |
| **Hybrid Connections** | App → on-prem resource via Service Bus relay |
| **Access Restrictions** | IP allow/deny list on the front door |
| **Service Endpoints** | Allow App Service subnet to reach Storage/SQL |
| **ASE (App Service Environment)** | Single-tenant deployment inside your VNet |

---

## 11. Diagnostics & Monitoring

- **Application Insights** → telemetry, live metrics, distributed tracing.
- **Log stream** → real-time stdout/stderr.
- **Diagnose and solve problems** → guided troubleshooter.
- **Kudu / SCM site** (`https://<app>.scm.azurewebsites.net`) → file explorer, console, process explorer.
- **App Service logs**: Application Logging (filesystem/Blob), Web Server Logging (IIS), Detailed errors, Failed request tracing.

---

## 12. WebJobs & Background Tasks

- **WebJobs** = scripts/programs that run inside the App Service VM.
- Types: **Continuous** (always on) and **Triggered** (manual/scheduled CRON).
- For continuous WebJobs enable **Always On** (Basic+).
- Modern alternative: **Azure Functions** or **Hosted Services** in ASP.NET Core.

---

## 13. Health Check

- App Service pings a path you specify (e.g., `/health`) every 1 min.
- Instances failing 2+ checks are taken out of load balancer rotation.
- Combine with `AddHealthChecks()` in ASP.NET Core.

```csharp
builder.Services.AddHealthChecks().AddSqlServer(connStr);
app.MapHealthChecks("/health");
```

---

## 14. Backup & Restore

- Standard+: scheduled or on-demand backup to **Storage Account**.
- Includes app content + linked DB (SQL/MySQL).
- Max 10 GB backup. Retain up to 30 days (or indefinitely).

---

## 15. Common App Settings

| Setting | Purpose |
|---|---|
| `WEBSITE_RUN_FROM_PACKAGE` | Read-only deploy from ZIP |
| `WEBSITE_NODE_DEFAULT_VERSION` | Pin Node version |
| `WEBSITES_ENABLE_APP_SERVICE_STORAGE` | For containers — mount persistent storage |
| `WEBSITES_PORT` | Container's listening port |
| `SCM_DO_BUILD_DURING_DEPLOYMENT` | Run build on Kudu after push |
| `ASPNETCORE_ENVIRONMENT` | Development/Staging/Production |

---

## 16. Containers on App Service

- Pull image from **ACR, Docker Hub, or any registry**.
- Single container or **Docker Compose** (multi-container, Linux only — being deprecated, prefer Container Apps).
- Use **Managed Identity → AcrPull** so App Service can pull without credentials.

```powershell
az webapp create -g rg1 -p plan1 -n myapp \
  --deployment-container-image-name myacr.azurecr.io/api:v1
az webapp identity assign -g rg1 -n myapp
# Grant the identity AcrPull on the registry
```

---

## 17. Common Pitfalls

| Pitfall | Fix |
|---|---|
| App slow on first request | Enable **Always On** (Basic+) |
| Slot swap moved my "Production" config to Staging | Mark settings as **slot setting** |
| 502.5 ANCM error | Wrong .NET runtime or unhandled startup exception |
| Container fails | Check `WEBSITES_PORT` matches container's listen port |
| Cannot reach SQL DB in VNet | Enable **VNet Integration** |
| Secret in plain text | Use **Key Vault reference** |
| Autoscale not working | Free/Shared/Basic tier — upgrade to Standard |

---

## 18. AZ-204 Interview/Exam Q&A

**Q1. Difference between App Service Plan and Web App?**
Plan = compute (VM+SKU); Web App = the app deployed onto it. One plan can host many apps.

**Q2. How do you achieve zero-downtime deployment?**
Deploy to **staging slot**, warm up, then **swap** with production.

**Q3. Where do you store secrets?**
**Key Vault** + Key Vault reference in app settings, or use **Managed Identity** to fetch at runtime.

**Q4. How does Easy Auth work?**
A platform module intercepts requests **before** they reach your code, validates tokens, and injects user info in `X-MS-CLIENT-PRINCIPAL`.

**Q5. Always On — why?**
By default idle apps are unloaded. Always On keeps the worker process alive so first-request latency stays low and continuous WebJobs run.

**Q6. How to connect to a private SQL DB from App Service?**
Enable **VNet Integration** on the app, then the SQL DB via **Private Endpoint** or service endpoint.

**Q7. Difference between scale up vs scale out?**
Up = bigger VM (vertical). Out = more VM instances (horizontal).

**Q8. Limits of free certs?**
App Service Managed Cert: free, auto-renew, **no wildcard, no apex on some scenarios**, only for custom domains added to the app.

**Q9. How to deploy from GitHub on each push?**
Use **GitHub Actions** workflow (auto-generated by Deployment Center) or **continuous deployment** in Deployment Center.

**Q10. Difference between WebJob and Function?**
WebJob runs **inside** the App Service VM (you pay for the plan). Function runs on **its own** plan (often Consumption — pay-per-execution).

---

## 19. Quick CLI Cheat Sheet

```powershell
# Create
az group create -n rg1 -l eastus
az appservice plan create -g rg1 -n plan1 --sku S1 --is-linux
az webapp create -g rg1 -p plan1 -n myapp --runtime "DOTNETCORE:9.0"

# Configure
az webapp config appsettings set -g rg1 -n myapp --settings KEY=VAL
az webapp config connection-string set -g rg1 -n myapp -t SQLAzure --settings Db="..."
az webapp config set -g rg1 -n myapp --always-on true --min-tls-version 1.2 --http20-enabled true

# Slots
az webapp deployment slot create -g rg1 -n myapp --slot staging
az webapp deployment slot swap   -g rg1 -n myapp --slot staging --target-slot production

# Logs
az webapp log tail -g rg1 -n myapp

# Restart
az webapp restart -g rg1 -n myapp
```

---

## 20. Mental Model

> **App Service = "Run my code, scale it, secure it, deploy it safely — I don't want to touch the VM."**
> Plan = the rented compute. Slots = staging clones for safe swaps. Easy Auth + Key Vault + VNet Integration = the holy trinity of secure App Service.
