# Azure Blob Storage (AZ-204)

> **One-liner**: Blob = massively scalable **object storage** for any unstructured data (images, videos, backups, logs).

---

## 1. Storage Account Hierarchy

```
Storage Account
└── Container (like a folder)
    └── Blob (the file itself)
```

- **Account name** must be globally unique, 3-24 lowercase alphanumeric.
- **Containers** are flat (no real subfolders — virtual via "/" in blob name).
- Endpoints: `https://<account>.blob.core.windows.net/<container>/<blob>`

---

## 2. Blob Types

| Type | Use case | Max size |
|---|---|---|
| **Block blob** | General files, images, backups | ~190 TiB |
| **Append blob** | Logs (append-only) | ~195 GiB |
| **Page blob** | VHDs, random read/write 512-byte pages | 8 TiB |

> Default & most common = **Block blob**.

---

## 3. Account Types & Performance

| Tier | Storage | Use |
|---|---|---|
| **Standard general-purpose v2** | HDD | Default |
| **Premium block blob** | SSD | High-IOPS small transactions |
| **Premium page blob** | SSD | VM disks |

---

## 4. Access Tiers (Block blobs)

| Tier | Cost/GB | Access cost | Min retention | Use |
|---|---|---|---|---|
| **Hot** | High | Low | None | Frequently accessed |
| **Cool** | Lower | Higher | **30 days** | Infrequent (>30d) |
| **Cold** | Even lower | Higher still | **90 days** | Rarely accessed |
| **Archive** (blob-level) | Lowest | Rehydrate required (hours) | **180 days** | Long-term backup |

> Use **Lifecycle Management** rules to auto-move blobs (e.g., Hot → Cool after 30 days, Archive after 90).

---

## 5. Redundancy Options

| Code | What it means | Region failure tolerance |
|---|---|---|
| **LRS** | Local (3 copies in 1 DC) | No |
| **ZRS** | 3 copies across 3 zones | Yes (zone-level) |
| **GRS** | LRS + async copy to paired region | Yes (read-only failover) |
| **RA-GRS** | GRS + read access on secondary | Yes |
| **GZRS** | ZRS + async copy | Best |
| **RA-GZRS** | GZRS + read on secondary | Best + readable |

---

## 6. Authentication Options

| Method | When to use |
|---|---|
| **Account key** | Dev/admin only — full access, hard to rotate |
| **Shared Access Signature (SAS)** | Time-limited, granular delegation |
| **Azure AD (Entra) RBAC** | **Recommended** — Managed Identity + role |
| **Anonymous public** | Public assets only (turn off if not needed) |

### Roles
- `Storage Blob Data Reader` — read
- `Storage Blob Data Contributor` — read/write/delete
- `Storage Blob Data Owner` — incl. POSIX ACLs (Data Lake)

---

## 7. SAS Types

| Type | Scope |
|---|---|
| **User Delegation SAS** | Signed with Entra credentials (preferred) |
| **Service SAS** | One service (blob/queue/etc.) |
| **Account SAS** | All services in account |

```csharp
var client = new BlobServiceClient(new Uri("https://acct.blob.core.windows.net"),
    new DefaultAzureCredential());
var userDelegationKey = await client.GetUserDelegationKeyAsync(
    DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));

var sasBuilder = new BlobSasBuilder
{
    BlobContainerName = "uploads",
    BlobName = "file.pdf",
    Resource = "b",
    ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
};
sasBuilder.SetPermissions(BlobSasPermissions.Read);
var sasToken = sasBuilder.ToSasQueryParameters(userDelegationKey, "acct").ToString();
```

---

## 8. .NET SDK Quickstart — `Azure.Storage.Blobs`

```csharp
// dotnet add package Azure.Storage.Blobs
// dotnet add package Azure.Identity

var cred = new DefaultAzureCredential();
var service = new BlobServiceClient(new Uri("https://acct.blob.core.windows.net"), cred);

var container = service.GetBlobContainerClient("uploads");
await container.CreateIfNotExistsAsync(PublicAccessType.None);

// Upload
var blob = container.GetBlobClient("hello.txt");
await blob.UploadAsync(BinaryData.FromString("Hello"), overwrite: true);

// Set metadata + tier
await blob.SetMetadataAsync(new Dictionary<string, string> { ["owner"] = "ana" });
await blob.SetAccessTierAsync(AccessTier.Cool);

// Download
var dl = await blob.DownloadContentAsync();
Console.WriteLine(dl.Value.Content.ToString());

// List
await foreach (var item in container.GetBlobsAsync())
    Console.WriteLine(item.Name);

// Delete
await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
```

---

## 9. Streaming & Large Files

- Use `OpenWriteAsync` / `OpenReadAsync` for large files.
- SDK does parallel block upload automatically.
- Tune `StorageTransferOptions.MaximumConcurrency` for throughput.

```csharp
var options = new BlobUploadOptions {
    TransferOptions = new StorageTransferOptions {
        InitialTransferSize = 4 * 1024 * 1024,
        MaximumTransferSize = 8 * 1024 * 1024,
        MaximumConcurrency = 8
    }
};
await blob.UploadAsync(stream, options);
```

---

## 10. Blob Metadata, Tags, Properties

| | Stored where | Searchable |
|---|---|---|
| **System properties** | Always | Limited |
| **Metadata** | Key/value headers | No (must enumerate) |
| **Blob Index Tags** | Key/value | **Yes — `FindBlobsByTags`** |

```csharp
await blob.SetTagsAsync(new Dictionary<string, string> { ["env"]="prod", ["type"]="invoice" });
await foreach (var hit in service.FindBlobsByTagsAsync("\"env\" = 'prod' AND \"type\" = 'invoice'"))
    Console.WriteLine(hit.BlobName);
```

---

## 11. Snapshots vs Versions vs Soft Delete

| Feature | What it does |
|---|---|
| **Snapshot** | Manual point-in-time copy (read-only) |
| **Versioning** | Auto creates a new version on every write |
| **Soft delete** | Retain deleted blob N days, then purge |
| **Point-in-time restore** | Restore container to a past time (needs versioning + change feed) |

---

## 12. Static Website Hosting

- Enable on Storage Account → "Static website".
- Serves from special `$web` container.
- Endpoint: `https://<account>.z##.web.core.windows.net`.
- Pair with Azure CDN / Front Door for custom domain + HTTPS.

---

## 13. Event Grid Integration

- Blob fires events: `BlobCreated`, `BlobDeleted`.
- Subscribe a **Function** / Logic App / Webhook to react.
- Common pipeline: upload → Event Grid → Function → process.

---

## 14. Lifecycle Management Rule (JSON)

```json
{
  "rules": [{
    "enabled": true,
    "name": "tier-down",
    "type": "Lifecycle",
    "definition": {
      "filters": { "blobTypes": ["blockBlob"], "prefixMatch": ["logs/"] },
      "actions": {
        "baseBlob": {
          "tierToCool":    { "daysAfterModificationGreaterThan": 30 },
          "tierToArchive": { "daysAfterModificationGreaterThan": 180 },
          "delete":        { "daysAfterModificationGreaterThan": 730 }
        }
      }
    }
  }]
}
```

---

## 15. Concurrency

- Use **ETags** (`If-Match`) for optimistic concurrency.
- Use **leases** (15s-60s or infinite) to coordinate writes.

```csharp
var lease = blob.GetBlobLeaseClient();
await lease.AcquireAsync(TimeSpan.FromSeconds(30));
try { /* mutate */ } finally { await lease.ReleaseAsync(); }
```

---

## 16. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Public anonymous access by accident | Set `AllowBlobPublicAccess=false` on account |
| Account key in code | Use **Managed Identity** + RBAC |
| Slow uploads | Increase concurrency, use SDK streaming |
| Listing 1M+ blobs slow | Use **Index Tags** + `FindBlobsByTags` |
| Archive blob 404 on download | Must **rehydrate** first (hours) |
| Soft-deleted by container — can't restore | Enable **container soft delete** separately |

---

## 17. AZ-204 Q&A

**Q1. Difference between block, append, and page blob?**
Block = generic files; Append = log-style append-only; Page = random-access 512-byte pages (VHDs).

**Q2. Which tier for "rarely read backups, must keep 7 years"?**
**Archive** (cheapest; rehydrate when needed). Combine with Lifecycle rule.

**Q3. How to grant temporary download access without an account key?**
Generate a **User Delegation SAS** (signed by Entra) with read permission.

**Q4. Best way to authenticate from App Service to Storage?**
**Managed Identity** + role `Storage Blob Data Contributor` on the container/account.

**Q5. How to trigger code when a blob is uploaded?**
**Event Grid** → Function (or BlobTrigger function).

**Q6. How to find all blobs with `env=prod`?**
Set **Index Tags**, then `FindBlobsByTags("\"env\"='prod'")`.

**Q7. Difference between snapshot and version?**
Snapshot = manual; Version = automatic on every write (when versioning enabled).

**Q8. GRS vs ZRS?**
GRS = async copy to paired region (geo-disaster). ZRS = sync across 3 zones in same region.

**Q9. What's the difference between Service SAS and User Delegation SAS?**
Service SAS is signed with the **account key**. User Delegation SAS is signed with **Entra credentials** (better — no key needed, easier to revoke).

**Q10. How to handle optimistic concurrency on a blob?**
Read ETag → pass `If-Match` on update. 412 = someone else changed it.

---

## 18. CLI Cheat Sheet

```powershell
# Create account
az storage account create -g rg1 -n acct1 --sku Standard_LRS --kind StorageV2 \
  --allow-blob-public-access false --min-tls-version TLS1_2

# Create container (Entra auth)
az storage container create -n uploads --account-name acct1 --auth-mode login

# Upload
az storage blob upload --account-name acct1 -c uploads -f local.txt -n hello.txt --auth-mode login

# Generate user delegation SAS
$expiry = (Get-Date).AddHours(1).ToUniversalTime().ToString("yyyy-MM-ddTHH:mmZ")
az storage blob generate-sas --account-name acct1 -c uploads -n hello.txt \
  --permissions r --expiry $expiry --auth-mode login --as-user
```

---

## 19. Mental Model

> **Blob = files in the cloud. Pick the tier by access frequency, pick redundancy by disaster tolerance, prefer Managed Identity over keys, use SAS for short-lived sharing, use Event Grid to react to changes.**
