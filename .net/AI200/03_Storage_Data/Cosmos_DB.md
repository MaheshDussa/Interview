# Azure Cosmos DB (AZ-204)

> **One-liner**: Globally-distributed, multi-model **NoSQL** database with single-digit-ms latency and **5 consistency levels**.

---

## 1. Key Concepts

```
Account
└── Database
    └── Container (partitioned, has RU/s)
        └── Item (JSON document, ≤ 2 MB)
```

- **RU/s** (Request Units per second) = currency for throughput.
- Charges by RU/s **provisioned** (or per-request in Serverless).

---

## 2. APIs (pick at account creation — cannot change later)

| API | Looks like | Use when |
|---|---|---|
| **NoSQL (Core)** | JSON + SQL-like queries | **Default / new apps** |
| **MongoDB** | Mongo wire protocol | Migrate Mongo apps |
| **Cassandra** | CQL | Migrate Cassandra |
| **Gremlin** | Graph traversal | Graph data |
| **Table** | Azure Table | Migrate Table storage |
| **PostgreSQL** | Distributed Postgres (Citus) | Relational at scale |

---

## 3. Partition Key — the #1 design decision

- Determines how items are distributed across physical partitions.
- **Logical partition** = all items with the same key (≤ 20 GB).
- **Physical partition** = backing storage (auto-managed).
- Choose a key with **high cardinality**, **even access**, used in **most queries**.

| Good | Bad |
|---|---|
| `/userId`, `/tenantId`, `/deviceId` | `/status` (few values, hot partition) |
| `/date` (if writes spread across many dates) | `/createdDate` for time-series (hot today) |

> **Hierarchical partition keys** (NoSQL API): up to 3 levels, e.g. `/tenantId`, `/userId`, `/sessionId`.

---

## 4. Throughput Modes

| Mode | Bill | When |
|---|---|---|
| **Provisioned (Manual)** | RU/s reserved per hour | Steady traffic |
| **Autoscale** | Pay between 10% and 100% of max | Spiky / unknown |
| **Serverless** | Pay per RU consumed | Dev/test, low/sporadic |

Container-level RU/s **or** Database-level shared RU/s (across containers).

---

## 5. Consistency Levels (5 — stronger → weaker)

| Level | Guarantee |
|---|---|
| **Strong** | Linearizable, single-region writes only |
| **Bounded staleness** | Lag bounded by K versions or T seconds |
| **Session** | (Default) Read-your-writes within same session |
| **Consistent prefix** | No gaps, but may be stale |
| **Eventual** | Cheapest, fastest, may be out of order |

**Memory hint**: **S**trong → **B**ounded → **S**ession → **C**onsistent prefix → **E**ventual = "SB-SCE".

> Lower consistency = lower RU cost + lower latency.

---

## 6. .NET SDK — `Microsoft.Azure.Cosmos` (v3)

```csharp
// dotnet add package Microsoft.Azure.Cosmos
// dotnet add package Azure.Identity

var client = new CosmosClient(
    "https://acct.documents.azure.com:443/",
    new DefaultAzureCredential(),
    new CosmosClientOptions { ApplicationName = "shop" });

var container = client.GetContainer("shop", "orders");

// Create
var order = new { id = Guid.NewGuid().ToString(), userId = "u1", total = 99.0m };
await container.CreateItemAsync(order, new PartitionKey("u1"));

// Read (point-read — cheapest, 1 RU)
var read = await container.ReadItemAsync<dynamic>("id-here", new PartitionKey("u1"));

// Query
var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @u")
    .WithParameter("@u", "u1");
using var iter = container.GetItemQueryIterator<dynamic>(query,
    requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey("u1") });
while (iter.HasMoreResults)
    foreach (var item in await iter.ReadNextAsync()) Console.WriteLine(item);

// Replace with ETag (optimistic concurrency)
await container.ReplaceItemAsync(order, order.id, new PartitionKey("u1"),
    new ItemRequestOptions { IfMatchEtag = etag });

// Patch (partial update)
await container.PatchItemAsync<dynamic>("id-here", new PartitionKey("u1"),
    new[] { PatchOperation.Set("/total", 120m) });

// Bulk
var clientBulk = new CosmosClient(endpoint, cred,
    new CosmosClientOptions { AllowBulkExecution = true });
```

---

## 7. Indexing

- **All properties indexed by default** (consistent indexing mode).
- Configure include/exclude paths to save RU.
- **Composite indexes** for ORDER BY across multiple fields.
- **Spatial indexes** for geo queries.
- **Vector indexes** (NoSQL API) for AI similarity search.

```json
{
  "indexingMode": "consistent",
  "includedPaths": [ { "path": "/*" } ],
  "excludedPaths": [ { "path": "/largeBlob/*" } ],
  "compositeIndexes": [[ { "path": "/userId", "order": "ascending" },
                         { "path": "/createdAt", "order": "descending" } ]]
}
```

---

## 8. TTL (Time To Live)

- Container-level TTL (seconds) — auto-delete stale items.
- Item-level `ttl` overrides.
- `ttl = -1` on container means TTL is enabled but disabled by default; set per-item.

---

## 9. Change Feed

- Persistent, ordered log of inserts/updates per partition.
- Read via:
  - **Change Feed Processor** (library) — stateful, scale-out, recommended.
  - **Azure Functions** Cosmos trigger (uses CFP internally).
  - **Pull model** (manual).
- **No delete events** by default — use **soft delete** + TTL pattern.

```csharp
[Function("OnOrder")]
public Task Run([CosmosDBTrigger(
    databaseName: "shop", containerName: "orders",
    Connection = "Cosmos",
    LeaseContainerName = "leases",
    CreateLeaseContainerIfNotExists = true)]
    IReadOnlyList<Order> changes) {
    foreach (var o in changes) /* react */;
    return Task.CompletedTask;
}
```

---

## 10. Global Distribution

- Replicate to any Azure region — single click.
- **Single-write region** (default) or **multi-region writes** (multi-master).
- SDK reads from nearest region automatically.
- **Automatic failover** option for DR.

---

## 11. Stored Procedures, Triggers, UDFs

- Written in **JavaScript**, run server-side.
- Sproc scope: **single logical partition**.
- Useful for atomic multi-doc operations.
- For most cases, use **transactional batch** (`TransactionalBatch`) from SDK.

```csharp
var batch = container.CreateTransactionalBatch(new PartitionKey("u1"))
    .CreateItem(orderA)
    .CreateItem(orderB);
var result = await batch.ExecuteAsync();
```

---

## 12. Security

| Concern | Approach |
|---|---|
| Keys | Avoid — use Entra auth + Managed Identity |
| Network | **Private Endpoint**, firewall, VNet service endpoints |
| Encryption | At rest (always), in transit (TLS), customer-managed keys (CMK) optional |
| RBAC | `Cosmos DB Built-in Data Contributor` for data plane |
| Always Encrypted | Client-side field encryption |

```powershell
# Grant data plane role to a Managed Identity
az cosmosdb sql role assignment create -g rg1 -a acct1 \
  --role-definition-id 00000000-0000-0000-0000-000000000002 \
  --principal-id <miObjectId> --scope /
```

---

## 13. Cost Optimization

- Use **point reads** (`ReadItemAsync`) — 1 RU vs queries.
- **Autoscale** for spiky loads.
- **Serverless** for dev/test.
- Tune **indexing policy** — exclude large/unused paths.
- Use **TTL** to drop stale data.
- **Reserved Capacity** (1-yr/3-yr) for 20-65% off.

---

## 14. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Hot partition (429s on one key) | Better partition key, autoscale, hierarchical PK |
| Cross-partition queries slow/expensive | Always pass `PartitionKey` in `QueryRequestOptions` |
| Account key leaked | Use Entra + MI |
| 20 GB logical partition limit | Pick higher-cardinality PK or hierarchical PK |
| ETag mismatch (412) | Re-read + retry |
| Change feed missed delete | Use soft-delete + TTL pattern |
| `id` not unique within partition | `id` is unique **per logical partition**, not globally |

---

## 15. AZ-204 Q&A

**Q1. What is an RU?**
Request Unit — abstracted cost of an operation (1 KB point read = ~1 RU). You provision RU/s.

**Q2. How do you choose a partition key?**
High cardinality, evenly distributed access, used in most queries, no single value carrying disproportionate traffic.

**Q3. 5 consistency levels?**
Strong, Bounded staleness, Session (default), Consistent prefix, Eventual.

**Q4. Difference between change feed and triggers?**
Change feed is durable & ordered, read by external code (Function/CFP). Triggers are JS, run server-side on operation.

**Q5. Point read vs query?**
Point read = `id`+PK, ~1 RU, fastest. Query may scan, costs more.

**Q6. Cosmos DB transaction scope?**
Limited to a **single logical partition** (transactional batch or sproc).

**Q7. How to keep cost low for dev?**
Use **Serverless** or **Free Tier** (1000 RU/s + 25 GB free per account).

**Q8. How to react to inserts?**
Cosmos DB **trigger Function** (uses change feed).

**Q9. Provisioned vs Autoscale?**
Provisioned = fixed RU/s. Autoscale = scales between 10% and max RU/s, billed per hour at peak used.

**Q10. How to do globally low-latency reads?**
Replicate to multiple regions, configure SDK `PreferredLocations` / multi-region writes.

---

## 16. CLI Cheat Sheet

```powershell
# Account
az cosmosdb create -g rg1 -n acct1 --kind GlobalDocumentDB \
  --locations regionName=eastus failoverPriority=0 isZoneRedundant=False \
  --default-consistency-level Session

# DB + container
az cosmosdb sql database create -g rg1 -a acct1 -n shop
az cosmosdb sql container create -g rg1 -a acct1 -d shop -n orders \
  --partition-key-path /userId --throughput 400

# Autoscale
az cosmosdb sql container throughput update -g rg1 -a acct1 -d shop -n orders \
  --max-throughput 4000

# Add region
az cosmosdb update -g rg1 -n acct1 \
  --locations regionName=eastus failoverPriority=0 \
  --locations regionName=westeurope failoverPriority=1
```

---

## 17. Mental Model

> **Cosmos DB = JSON + partition key + RU/s + 5 consistency levels + global replication. Get the partition key right and everything else falls into place.**
