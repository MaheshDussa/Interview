# AZ-204 — Exam Pattern Questions

> 100+ realistic AZ-204 questions in the style Microsoft asks. Format: multiple-choice / scenario / drag-drop. **Answers + explanations** at the bottom of each section.

> Topics map to the official exam outline:
> 1. App Service Web Apps
> 2. Azure Functions
> 3. Blob Storage
> 4. Cosmos DB
> 5. Containerized Solutions
> 6. Authentication & Authorization
> 7. Secure Solutions (Key Vault, MI, Config)
> 8. API Management, Event Grid, Service Bus, Caching
> 9. Monitoring (App Insights)

---

## Section 1 — App Service (12 Qs)

**Q1.** You need zero-downtime deployments and the ability to roll back in seconds. Which feature?
- A. Scale out
- B. Always On
- C. **Deployment slots + swap**
- D. WebJobs

**Q2.** Free App Service Managed Certificates support which domain types?
- A. Wildcard
- B. **Custom domain added to the app (non-wildcard)**
- C. Apex domains with naked records only
- D. Any domain on the internet

**Q3.** You marked `ConnectionStrings:Sql` as **Slot setting** then swapped staging→production. After the swap, which value is now in production?
- A. The staging value
- B. **The previous production value** (slot setting stays with the slot)
- C. Empty
- D. Both merged

**Q4.** A continuous WebJob stops running when traffic is low. What setting fixes it?
- A. Increase Pricing tier
- B. Enable **Always On**
- C. Add a Health check
- D. Enable Autoscale

**Q5.** Your container app on App Service returns 503. Logs show it never started. Most likely cause?
- A. Missing slot setting
- B. **`WEBSITES_PORT` doesn't match the container's listen port**
- C. ASP.NET Core Hosting Bundle missing
- D. CORS not configured

**Q6.** Cheapest tier that supports autoscale + slots?
- A. F1
- B. B1
- C. **S1**
- D. P1v3

**Q7.** Which combination keeps secrets out of `appsettings.json`?
- A. Encrypted file
- B. **Managed Identity + Key Vault reference**
- C. Hard-code in code
- D. Pass via query string

**Q8.** You want App Service to reach a private SQL DB. Choose:
- A. Public endpoint + firewall rule on SQL
- B. **VNet Integration (out) + Private Endpoint on SQL**
- C. Service Endpoint on App Service public IP
- D. Front Door

**Q9.** Drag-drop: order to enable HTTPS-only with managed cert.
1. Add custom domain
2. Validate DNS
3. Bind certificate
4. Toggle HTTPS Only
Order: **1 → 2 → 3 → 4**

**Q10.** Best way to deploy from GitHub on every push without secrets?
- A. FTP credentials
- B. Publish profile in secret
- C. **OIDC federated credential**
- D. Manual ZIP upload

**Q11.** You need the file system isolated and read-only post-deploy. Use:
- A. ZIP deploy
- B. Local Git
- C. **Run From Package (`WEBSITE_RUN_FROM_PACKAGE=1`)**
- D. WebDeploy

**Q12.** App Service Authentication ("Easy Auth") rejects unauth requests. The platform passes user info via:
- A. Cookie only
- B. `Authorization` header rewritten
- C. **`X-MS-CLIENT-PRINCIPAL` header**
- D. URL query string

---

## Section 2 — Azure Functions (14 Qs)

**Q13.** Max execution time on Consumption plan?
- A. 1 minute
- B. 5 min default, 10 min max
- C. 30 min
- D. Unlimited

**Q14.** A function must process **10 GB files** stored in Blob. Best trigger?
- A. Blob trigger (Consumption)
- B. **Event Grid trigger** (BlobCreated → function)
- C. Timer trigger
- D. Queue trigger

**Q15.** Which plan gives no cold start + VNet integration?
- A. Consumption
- B. **Premium**
- C. Dedicated free tier
- D. Serverless Cosmos

**Q16.** A durable orchestrator uses `DateTime.UtcNow`. It works in tests but produces wrong results in prod. Fix:
- A. Add retry policy
- B. Use **`ctx.CurrentUtcDateTime`**
- C. Use static field
- D. Increase plan tier

**Q17.** Function key authorization levels — which requires the host master key?
- A. Function
- B. **Admin**
- C. Anonymous
- D. User

**Q18.** Pick the correct attribute for an isolated worker function:
- A. `[FunctionName("X")]`
- B. **`[Function("X")]`**
- C. `[AzureFunction("X")]`
- D. `[Trigger("X")]`

**Q19.** A function app needs to read secrets from Key Vault. Easiest secure approach?
- A. Use a stored connection string in code
- B. **Enable system-assigned MI + grant `Key Vault Secrets User` role**
- C. Use a SAS token
- D. Use the master key

**Q20.** Which Durable pattern fits "send approval link, wait up to 7 days for click"?
- A. Function chaining
- B. Fan-out/fan-in
- C. **Human interaction (external event with timeout)**
- D. Monitor

**Q21.** A Cosmos DB trigger fails to scale. Cause?
- A. Wrong key
- B. **Lease container misconfigured**
- C. Function disabled
- D. Wrong region

**Q22.** Pick the storage role required for `AzureWebJobsStorage` connection (with MI):
- A. Storage Blob Data Reader
- B. Storage Account Contributor
- C. **Storage Blob Data Owner + Storage Queue Data Contributor + Storage Table Data Contributor**
- D. None — only key works

**Q23.** What's true about bindings?
- A. Each function can have many triggers
- B. **Each function has exactly one trigger**
- C. Bindings only support input
- D. Bindings must use account keys

**Q24.** Best plan for an event-driven worker that should scale to zero?
- A. Dedicated
- B. **Consumption / Flex**
- C. Always App Service Standard
- D. Premium fixed-min instances

**Q25.** A function must run every Monday 06:00 UTC. CRON?
- A. `0 0 6 * * 1`
- B. **`0 0 6 * * 1`** (sec min hour day month dow — Mon=1)
- C. `0 6 * * 1`
- D. `0 0 0 6 1 *`

**Q26.** When does scale controller add workers in Consumption?
- A. CPU > 90%
- B. **Based on trigger metrics (queue length, event lag, HTTP RPS)**
- C. Manual only
- D. Memory > 80%

---

## Section 3 — Blob Storage (12 Qs)

**Q27.** Best access tier for "rarely-read 7-year retention backup"?
- A. Hot
- B. Cool
- C. **Archive**
- D. Premium

**Q28.** Generate a temporary read URL without using the account key:
- A. Service SAS
- B. **User Delegation SAS**
- C. Account SAS
- D. Anonymous public

**Q29.** A blob is in Archive. You need to read it. What first?
- A. Delete and re-upload
- B. **Rehydrate to Hot or Cool**
- C. Copy to another container
- D. Use SAS

**Q30.** You need to find all blobs where `env=prod`. Use:
- A. Metadata enumerate
- B. **Blob Index Tags + `FindBlobsByTags`**
- C. Container listing
- D. Soft-delete query

**Q31.** Best redundancy for "zone failure tolerance, single region"?
- A. LRS
- B. **ZRS**
- C. GRS
- D. RA-GRS

**Q32.** Which blob type for append-only log streaming?
- A. Block
- B. **Append**
- C. Page
- D. Premium

**Q33.** Default lifecycle action: auto-move logs to Cool after 30 days. Which feature?
- A. Versioning
- B. Snapshot
- C. **Lifecycle Management policy**
- D. Soft delete

**Q34.** Storage account uses MI from an App Service. Minimum role for upload?
- A. Reader
- B. **Storage Blob Data Contributor**
- C. Storage Account Contributor
- D. Owner

**Q35.** Optimistic concurrency on a blob:
- A. Use SAS
- B. **Pass `If-Match: <etag>` on PUT**
- C. Use lease
- D. Use snapshot

**Q36.** Trigger code on every blob upload, near real-time:
- A. Polling timer
- B. **Event Grid → Function**
- C. Storage queue + cron
- D. Manual SAS

**Q37.** You see 429 throttling. First fix:
- A. Reduce concurrency
- B. Switch to Premium
- C. **Use exponential back-off retries; spread across partitions**
- D. Disable encryption

**Q38.** Static website hosting serves from container:
- A. `web`
- B. `static`
- C. **`$web`**
- D. `root`

---

## Section 4 — Cosmos DB (12 Qs)

**Q39.** Currency of throughput?
- A. DTU
- B. **RU/s**
- C. vCore
- D. IOPS

**Q40.** Default consistency level?
- A. Strong
- B. Eventual
- C. **Session**
- D. Bounded staleness

**Q41.** Best partition key for tenant-isolated SaaS?
- A. `/status`
- B. **`/tenantId`**
- C. `/createdAt`
- D. `/id`

**Q42.** Cheapest operation:
- A. Cross-partition query
- B. **Point read by `id` + PK**
- C. Aggregation
- D. ORDER BY

**Q43.** You need read-your-writes for the same user but ok across users:
- A. Strong
- B. **Session**
- C. Eventual
- D. Consistent prefix

**Q44.** Transactional batch scope?
- A. Cross-DB
- B. Cross-container
- C. Cross-partition
- D. **Single logical partition**

**Q45.** A change feed reader missed deletes. Solution:
- A. Use trigger
- B. **Soft-delete + TTL pattern**
- C. Enable strong consistency
- D. Use Cassandra API

**Q46.** Indexing default mode:
- A. None
- B. Lazy
- C. **Consistent (all paths indexed)**
- D. Manual only

**Q47.** Choose throughput mode for dev/test sporadic usage:
- A. Provisioned 400 RU/s
- B. **Serverless**
- C. Autoscale
- D. Dedicated

**Q48.** Multi-region writes (multi-master) — when?
- A. Strong consistency required
- B. **Low write latency for users worldwide**
- C. Lowest RU cost
- D. Default

**Q49.** A logical partition is approaching 20 GB. Fix:
- A. Resize container
- B. **Repartition with a higher-cardinality PK / hierarchical PK**
- C. Switch to Cassandra
- D. Enable autoscale

**Q50.** Authenticate from MI to data plane — role:
- A. DocumentDB Account Contributor
- B. **Cosmos DB Built-in Data Contributor (`00000000-0000-0000-0000-000000000002`)**
- C. Reader
- D. Owner

---

## Section 5 — Containers (10 Qs)

**Q51.** Serverless containers with scale-to-zero + KEDA:
- A. ACI
- B. **Azure Container Apps**
- C. AKS
- D. App Service

**Q52.** Run a one-off batch job in a container with per-second billing:
- A. AKS
- B. **ACI**
- C. App Service
- D. Logic Apps

**Q53.** Pull from ACR with no stored credentials:
- A. Admin user enabled
- B. **Managed Identity + AcrPull role**
- C. SAS
- D. Public registry

**Q54.** ACA traffic split for canary:
- A. Multi-region writes
- B. **Multiple revisions + traffic weights**
- C. Auto-rollback only
- D. Front Door rule

**Q55.** AKS attach ACR command:
- A. `az acr import`
- B. **`az aks update --attach-acr <name>`**
- C. `az aks credential set`
- D. None — manual secret

**Q56.** ACA scale rule for queue length 5:
- A. HTTP rule
- B. CPU rule
- C. **Custom Service Bus rule with `messageCount=5`**
- D. Cron rule

**Q57.** AKS pod-level Azure identity (modern):
- A. Pod Managed Identity (deprecated)
- B. **Workload Identity**
- C. Kubernetes secret
- D. Helm values

**Q58.** AGIC stands for:
- A. Azure Gateway Identity Controller
- B. **Application Gateway Ingress Controller**
- C. AKS Gateway Identity Container
- D. Azure Global Image Cache

**Q59.** Dockerfile best practice — multi-stage build for .NET:
- A. Run `dotnet build` in final image
- B. **Compile in `sdk` image, copy publish output into `aspnet` image**
- C. Use Alpine only
- D. Always use `latest` tag

**Q60.** Smallest unit deployable in K8s:
- A. Container
- B. **Pod**
- C. Deployment
- D. Service

---

## Section 6 — Authentication & Authorization (12 Qs)

**Q61.** Flow for an SPA today:
- A. Implicit
- B. Client Credentials
- C. **Authorization Code + PKCE**
- D. ROPC

**Q62.** Flow for daemon → Microsoft Graph:
- A. Auth Code
- B. **Client Credentials**
- C. OBO
- D. Device Code

**Q63.** API A receives a user token and calls API B as the user:
- A. Client Credentials
- B. **On-Behalf-Of**
- C. Refresh token grant
- D. SAML

**Q64.** Difference between `scp` and `roles` claims:
- A. Same thing
- B. **`scp` = delegated scopes; `roles` = app roles (or app-only roles)**
- C. `scp` for SAML, `roles` for OAuth
- D. Only `roles` exists

**Q65.** Validate a JWT — must check:
- A. Only `exp`
- B. Only signature
- C. **Signature, `iss`, `aud`, `exp`, `nbf`**
- D. Audience only

**Q66.** `DefaultAzureCredential` order includes:
- A. Only env vars
- B. **Env → Workload Identity → Managed Identity → CLI → VS → Interactive**
- C. Only managed identity
- D. Only interactive

**Q67.** Easy Auth flow for App Service:
- A. App code validates token
- B. **Platform validates token; app reads `X-MS-CLIENT-PRINCIPAL`**
- C. Cookies only
- D. Disabled by default

**Q68.** Token lifetime defaults:
- A. Access 5 min
- B. **Access ~1 hr, refresh ~24h-90d**
- C. Both 24 hrs
- D. ID token 1 week

**Q69.** B2C is for:
- A. Internal employees
- B. **External consumers with custom-branded sign-up**
- C. Guest users from partner orgs
- D. Service principals

**Q70.** Conditional Access purpose:
- A. Manage RBAC
- B. **Policies controlling sign-in conditions (MFA, location, device)**
- C. Encrypt tokens
- D. Replace OAuth

**Q71.** Best secret for confidential client app:
- A. Plain secret
- B. **Certificate** (or MI)
- C. SAS token
- D. Cookie

**Q72.** PKCE protects against:
- A. Replay of ID token
- B. **Authorization code interception**
- C. Token theft
- D. XSS

---

## Section 7 — Secure Solutions (10 Qs)

**Q73.** Both RBAC and access policies enabled on KV. What happens?
- A. Both apply
- B. **Vault uses only the selected model — choose one**
- C. RBAC wins
- D. Policy wins

**Q74.** App Service connects to KV. Best identity:
- A. Account key
- B. **System-assigned Managed Identity**
- C. Service principal with secret
- D. SAS

**Q75.** Encrypt blobs with your own RSA key:
- A. Not supported
- B. **CMK in Key Vault assigned to storage account**
- C. Use SAS
- D. Use TLS only

**Q76.** Private Endpoint key dependency:
- A. NSG
- B. **Private DNS zone**
- C. Service tag
- D. Route table

**Q77.** Centralize logs across resources:
- A. Activity Log only
- B. **Diagnostic settings → Log Analytics workspace**
- C. Storage account only
- D. Event Hub only

**Q78.** Enforce TLS 1.2 minimum on Storage:
- A. Not configurable
- B. **`Min TLS = 1.2` setting on account**
- C. NSG rule
- D. Firewall rule

**Q79.** Defender for Cloud purpose:
- A. SIEM
- B. **CSPM + workload protection for Azure resources**
- C. Pen testing
- D. Identity provider

**Q80.** Sentinel purpose:
- A. CSPM
- B. **SIEM/SOAR — incidents + playbooks from logs**
- C. Backups
- D. Firewall

**Q81.** Key Vault soft delete cannot be turned off because:
- A. It's optional
- B. **It's mandatory since 2020**
- C. Only premium has it
- D. Only RBAC has it

**Q82.** Best practice for App Service Function reading Key Vault secret as env var:
- A. Hardcode SecretUri value
- B. **`@Microsoft.KeyVault(SecretUri=...)` reference + MI**
- C. Use ARM template parameters
- D. None — Azure can't do this

---

## Section 8 — APIM / Messaging / Cache (12 Qs)

**Q83.** API Management policy to throttle 100 calls/min per subscription:
- A. `set-backend-service`
- B. **`rate-limit-by-key`**
- C. `cache-store`
- D. `cors`

**Q84.** APIM policy for caching responses:
- A. `set-header`
- B. `validate-jwt`
- C. **`cache-lookup` + `cache-store`**
- D. `rewrite-uri`

**Q85.** Service Bus vs Storage Queue — pick SB when:
- A. You need cheapest queue
- B. **Need FIFO, sessions, dead-lettering, transactions, topics/sub**
- C. >80 GB queue
- D. Free tier

**Q86.** Event Grid vs Event Hub:
- A. Same thing
- B. **Event Grid = discrete reactive events (pub/sub); Event Hub = high-throughput streaming/telemetry**
- C. Hub is for emails
- D. Grid is paid only

**Q87.** Service Bus message that can't be processed after N retries goes to:
- A. Original queue
- B. **Dead-Letter Queue (DLQ)**
- C. Storage
- D. Event Grid

**Q88.** Choose Cosmos change feed vs Service Bus for "downstream microservices react to order create":
- A. Always SB
- B. **Either — Cosmos change feed if data already in Cosmos; SB if you need ordering, retries, multiple sub independence**
- C. Only Cosmos
- D. Only SB

**Q89.** Redis cache pattern: read DB only on cache miss = **Cache-aside**.

**Q90.** Redis eviction policy when memory full and you want recently-used items kept:
- A. `allkeys-random`
- B. **`allkeys-lru`**
- C. `noeviction`
- D. `volatile-ttl`

**Q91.** Event Grid delivery guarantees:
- A. Exactly once
- B. **At least once with retry (up to 24h)**
- C. At most once
- D. No retry

**Q92.** Service Bus session enables:
- A. HTTPS
- B. **FIFO across a session-id**
- C. Bigger messages
- D. Free tier

**Q93.** APIM SKU with VNet injection + multi-region:
- A. Consumption
- B. Basic
- C. Standard
- D. **Premium** (or Standard v2 self-hosted gateway)

**Q94.** Storage queue max message size:
- A. 256 KB
- B. **64 KB**
- C. 1 MB
- D. 4 MB

---

## Section 9 — Monitoring & App Insights (8 Qs)

**Q95.** Add App Insights to ASP.NET Core:
- A. Manual log writer
- B. **`builder.Services.AddApplicationInsightsTelemetry();` + connection string**
- C. Use Event Hub
- D. Use NLog only

**Q96.** Distributed tracing across services — feature:
- A. Live Metrics
- B. **Operation Id / W3C Trace Context auto-propagation**
- C. Snapshot debugger
- D. Profiler

**Q97.** KQL — top 5 slowest dependencies:
```kusto
dependencies
| summarize avg(duration) by name
| top 5 by avg_duration desc
```

**Q98.** Cap App Insights cost:
- A. Disable
- B. **Sampling + Daily Cap**
- C. Switch SKU
- D. Use only Live Metrics

**Q99.** Custom metric:
- A. `TrackEvent`
- B. **`TrackMetric` / `Metric` API**
- C. `TrackException`
- D. `TrackDependency`

**Q100.** Live Metrics use case:
- A. Long-term retention
- B. **Real-time near-zero-latency view of incoming telemetry**
- C. Cost analysis
- D. Alerts

**Q101.** Alert on "5xx > 5% in 5 min":
- A. Activity Log alert
- B. **Metric alert / Log alert on App Insights**
- C. Service Health alert
- D. Cost alert

**Q102.** Snapshot Debugger:
- A. Records videos
- B. **Captures snapshots of exceptions in production for debugging**
- C. Compiles code
- D. Cleans logs

---

## Section 10 — Scenario / Drag-Drop (8 Qs)

**Q103.** **Scenario**: A web app must (a) scale based on queue length, (b) stop costing money at idle, (c) integrate with VNet. Best service?
**Answer**: **Azure Container Apps** (KEDA scale rule + scale-to-zero + VNet).

**Q104.** **Scenario**: Public API needs WAF + global anycast + caching at edge.
**Answer**: **Azure Front Door (Premium) with WAF**.

**Q105.** **Scenario**: A multi-region Cosmos workload needs single-digit-ms writes worldwide.
**Answer**: Enable **multi-region writes (multi-master)**; pick **Session** consistency.

**Q106.** **Scenario**: Function App needs to read 50k blob events/sec.
**Answer**: Use **Event Grid trigger** (push) on **Premium plan** (not Blob polling trigger).

**Q107.** **Scenario**: Replace storage account key in code.
**Answer**: Enable **System-assigned MI** on the consumer → assign `Storage Blob Data Contributor` → use `DefaultAzureCredential`.

**Q108.** **Drag-drop**: Configure Private Endpoint for Storage.
1. Create Private Endpoint resource pointing at the storage account
2. Approve the connection on the storage account
3. Create / link **Private DNS Zone** (`privatelink.blob.core.windows.net`)
4. Disable **public network access** on the storage account
Order: **1 → 2 → 3 → 4**

**Q109.** **Scenario**: Need long-running orchestration (>10 min) with branching + retries on a budget.
**Answer**: **Durable Functions** on Premium (or Consumption if each activity <10 min).

**Q110.** **Drag-drop**: Add Key Vault secret to App Service.
1. Enable system-assigned MI on App Service
2. Grant `Key Vault Secrets User` role on the vault (RBAC mode)
3. Add app setting value `@Microsoft.KeyVault(SecretUri=...)`
4. Restart app
Order: **1 → 2 → 3 → 4**

---

## How to study

1. Read the topic file (e.g., [AppService_WebApps.md](../01_Compute/AppService_WebApps.md)) → answer that section here.
2. For every wrong answer, re-read the corresponding section of the topic file.
3. Practice CLI: spin up resources in a sandbox subscription.
4. Memorize the **mental model** one-liner at the end of each topic file — those map directly to exam scenarios.

---

## Mental Model

> **AZ-204 = "Can you build, secure, deploy, and monitor a cloud-native .NET app using App Service, Functions, Storage, Cosmos, Containers, Entra, Key Vault, APIM, and App Insights?"**
