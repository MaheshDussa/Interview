# Azure Container Registry (ACR) — Self-Explanatory Notes

> A clean, beginner-friendly walkthrough: **what it is, why it matters, how to use it, and how it fits into a real CI/CD pipeline.**

---

## 1. What is ACR?

**Azure Container Registry** is a **private registry for container images and OCI artifacts** (Docker images, Helm charts, OCI artifacts) hosted in Azure.

Think of it as **"GitHub for Docker images" inside your Azure tenant** — secure, private, integrated with Azure identity (Entra ID / RBAC), regional, and replicated if needed.

| Public registry (Docker Hub) | ACR |
|---|---|
| Anyone can pull (rate-limited) | Private, you control access |
| No Azure identity integration | Entra ID, Managed Identity, RBAC |
| Limited features | Tasks, geo-replication, scanning, content trust |

---

## 2. Why use ACR?

- **Private** — store proprietary images without leaking them publicly.
- **Close to compute** — same region as AKS / App Service / Container Apps = faster pulls, lower egress cost.
- **Identity-based access** — no static passwords; use Managed Identity.
- **Build in the cloud** — ACR Tasks build images without a local Docker daemon.
- **Compliance** — content trust (signed images), vulnerability scanning (Defender for Cloud).
- **Geo-replication** — one registry, many regions; closest pull automatically.

---

## 3. Core concepts (vocabulary)

| Term | Meaning |
|---|---|
| **Registry** | The whole ACR resource, e.g. `myregistry.azurecr.io` |
| **Repository** | A named collection of images, e.g. `myregistry.azurecr.io/orders-api` |
| **Image** | One build of an app inside a repository |
| **Tag** | A human-friendly label on an image, e.g. `:v1.2`, `:latest` |
| **Digest** | Immutable SHA256 hash that uniquely identifies an image |
| **Manifest** | Metadata describing layers + platform of an image |
| **Layer** | A diff filesystem chunk that makes up an image (shared between images) |
| **Task** | Server-side build/automation job in ACR |
| **Token / Scope map** | Fine-grained access (read/write specific repos) |

Full image reference:
```
myregistry.azurecr.io/orders-api:1.2.3
└────────┬────────┘ └────┬────┘ └──┬──┘
       registry      repository   tag
```

---

## 4. SKU tiers (which one?)

| SKU | Storage | Geo-replication | Private link | Tasks | When to choose |
|---|---|---|---|---|---|
| **Basic** | 10 GB | No | No | Yes | Dev / small projects |
| **Standard** | 100 GB | No | No | Yes | Most production workloads |
| **Premium** | 500 GB+ | **Yes** | **Yes** | Yes | Enterprise, multi-region, private networking, content trust, tokens |

Rule of thumb: start **Standard**, upgrade to **Premium** when you need geo-replication, private endpoint, or scope tokens.

---

## 5. Creating an ACR (the 30-second tour)

```powershell
# 1. Login to Azure
az login

# 2. Create a resource group (if needed)
az group create -n rg-demo -l eastus

# 3. Create the registry (name must be globally unique, lowercase, 5-50 chars)
az acr create `
  -n myregistry123 `
  -g rg-demo `
  --sku Standard `
  --admin-enabled false
```

> **Don't** enable the admin user in production — it's a static username/password. Prefer **Managed Identity** or **service principal** auth.

---

## 6. Pushing your first image

```powershell
# 1. Authenticate Docker with ACR using your Azure login (no password!)
az acr login -n myregistry123

# 2. Build a local image
docker build -t orders-api:1.0 .

# 3. Tag it for the registry
docker tag orders-api:1.0 myregistry123.azurecr.io/orders-api:1.0

# 4. Push
docker push myregistry123.azurecr.io/orders-api:1.0

# 5. Verify
az acr repository list -n myregistry123
az acr repository show-tags -n myregistry123 --repository orders-api
```

---

## 7. Pulling images

### From your laptop
```powershell
az acr login -n myregistry123
docker pull myregistry123.azurecr.io/orders-api:1.0
```

### From AKS (recommended — Managed Identity)
```powershell
# Attach ACR to AKS once; AKS nodes get AcrPull role
az aks update -n my-aks -g rg-demo --attach-acr myregistry123
```
After that, any pod referencing `myregistry123.azurecr.io/...` just works — no pull secrets in YAML.

### From App Service / Container Apps
Enable **System-assigned Managed Identity** on the app, then grant it `AcrPull` on the registry. Configure the container image source to ACR.

---

## 8. ACR Tasks — build in the cloud

You don't need Docker installed locally. ACR can build directly from your source.

### Quick task (one-off build)
```powershell
az acr build `
  -r myregistry123 `
  -t orders-api:{{.Run.ID}} `
  .
```

### Triggered task (build on every git push)
```powershell
az acr task create `
  --name build-orders-api `
  --registry myregistry123 `
  --image orders-api:{{.Run.ID}} `
  --context https://github.com/me/orders-api.git `
  --file Dockerfile `
  --branch main `
  --git-access-token <PAT>
```

Useful task types:
- **Quick task** — one-shot build.
- **Git task** — auto-rebuild on commit.
- **Base image update** — auto-rebuild when the base image (e.g. `mcr.microsoft.com/dotnet/aspnet:9.0`) is patched. **Huge security win.**
- **Schedule** — nightly rebuilds for fresh CVE patches.
- **Multi-step** — YAML pipeline (build, test, push, scan).

---

## 9. Authentication options (ranked best → worst)

1. **Managed Identity** of the consumer (AKS, App Service, VM) + `AcrPull` role. Zero secrets.
2. **User-assigned Managed Identity** shared by many resources.
3. **Service Principal** with `AcrPull` / `AcrPush` (use only when MI not available).
4. **Repository-scoped tokens** (Premium) — fine-grained, expiring.
5. **Admin user** — avoid in production.

---

## 10. RBAC roles cheat sheet

| Role | Pull | Push | Delete | Build (Tasks) |
|---|:-:|:-:|:-:|:-:|
| `AcrPull` | ✅ | ❌ | ❌ | ❌ |
| `AcrPush` | ✅ | ✅ | ❌ | ❌ |
| `AcrDelete` | ❌ | ❌ | ✅ | ❌ |
| `Contributor` | ✅ | ✅ | ✅ | ✅ |
| `AcrImageSigner` | — | — | — | content trust signing |

Assign at registry scope (or repository scope with Premium tokens).

---

## 11. Networking & security

- **Public endpoint** on by default — accessible from anywhere with credentials.
- **Private Endpoint** (Premium) — registry only reachable from inside your VNet.
- **Firewall rules** — IP allowlist + selected VNets.
- **Disable public network access** when using Private Endpoint.
- **Trusted services bypass** — let AKS / Defender reach it even when public is off.

```powershell
# Lock down to private only
az acr update -n myregistry123 --public-network-enabled false
```

---

## 12. Image hygiene & lifecycle

Without cleanup, repos grow forever (and cost $$$).

### Manual delete
```powershell
az acr repository delete -n myregistry123 --image orders-api:1.0
```

### Retention policy (Premium)
- Auto-delete **untagged manifests** after N days.
```powershell
az acr config retention update -r myregistry123 --status Enabled --days 30 --type UntaggedManifests
```

### Lock production tags
```powershell
az acr repository update `
  --name myregistry123 `
  --image orders-api:prod `
  --write-enabled false --delete-enabled false
```

### Best practices
- Tag with **immutable** versions (`1.2.3`, git SHA) — not just `latest`.
- Use `latest` only for local dev convenience.
- Reference images by **digest** (`@sha256:...`) in production manifests for immutability.
- Multi-stage Dockerfiles → smaller images → faster pulls.

---

## 13. Security features

| Feature | What it does |
|---|---|
| **Defender for Containers** | Scans pushed images for CVEs; surfaces in Microsoft Defender for Cloud |
| **Content Trust (Notary v1)** | Sign and verify images; pull only signed ones |
| **Image quarantine** (preview) | New images locked until scanned clean |
| **Customer-managed keys (CMK)** | Encrypt registry with your own Key Vault key |
| **Diagnostic logs** | Push login attempts, pulls, pushes to Log Analytics |

---

## 14. Geo-replication (Premium)

One registry, replicas in multiple regions. Same login URL, Azure picks the closest replica automatically.

```powershell
az acr replication create -r myregistry123 -l westeurope
az acr replication create -r myregistry123 -l southeastasia
az acr replication list   -r myregistry123 -o table
```

Use cases:
- Multi-region AKS clusters pull from local replica → faster, no cross-region egress.
- Disaster recovery (a region outage doesn't stop deployments).

---

## 15. CI/CD with GitHub Actions (minimal example)

```yaml
name: build-push
on:
  push: { branches: [ main ] }

jobs:
  build:
    runs-on: ubuntu-latest
    permissions:
      id-token: write    # OIDC
      contents: read
    steps:
      - uses: actions/checkout@v4

      - uses: azure/login@v2
        with:
          client-id:    ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id:    ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: ACR login
        run: az acr login -n myregistry123

      - name: Build & push
        run: |
          IMAGE=myregistry123.azurecr.io/orders-api:${{ github.sha }}
          docker build -t $IMAGE .
          docker push $IMAGE
```

**OIDC** (workload identity federation) is the modern, secretless way. No `AZURE_CREDENTIALS` JSON, no rotating client secrets.

---

## 16. Useful Azure CLI cheat sheet

```powershell
# List registries
az acr list -o table

# List repos in a registry
az acr repository list -n myregistry123 -o table

# Tags in a repo
az acr repository show-tags -n myregistry123 --repository orders-api -o table

# Inspect a specific manifest/digest
az acr repository show-manifests -n myregistry123 --repository orders-api --detail

# Delete a tag (keep underlying manifest)
az acr repository untag -n myregistry123 --image orders-api:old

# Import an image from another registry (no local docker needed)
az acr import -n myregistry123 `
  --source docker.io/library/redis:7 `
  --image redis:7

# Run an image directly in ACR (great for smoke tests)
az acr run -r myregistry123 --cmd "$Registry/orders-api:1.0" /dev/null

# Show registry usage
az acr show-usage -n myregistry123
```

---

## 17. Costs — what actually charges you

- **Storage** per GB (each SKU includes a base amount; overage billed per GB-month).
- **Network egress** when pulling outside Azure (intra-region pulls = free).
- **Geo-replication** — pay per replica region.
- **ACR Tasks** — per build-minute (Linux/Windows differ).
- **Private Endpoint** — small per-hour + data charges.

Save money: keep ACR in the **same region** as your compute, prune old images, prefer **digest pulls** for layer cache reuse.

---

## 18. Common pitfalls (and fixes)

| Symptom | Likely cause | Fix |
|---|---|---|
| `unauthorized: authentication required` | Not logged in / wrong tenant | `az acr login -n <name>` after `az account set` |
| `denied: requested access to the resource is denied` | Identity lacks `AcrPull`/`AcrPush` | Assign correct role at registry scope |
| AKS pods stuck `ImagePullBackOff` | ACR not attached to AKS | `az aks update --attach-acr` |
| Push very slow from laptop | Big layers, distant region | Use ACR Tasks (`az acr build`) — pushes layers cloud-side |
| Image vulnerable but not detected | Defender for Containers not enabled | Turn on in Microsoft Defender for Cloud |
| `latest` deployed wrong version | Tag mutation race | Deploy by digest or unique tag |
| Premium feature not working | Registry is Basic/Standard | `az acr update --sku Premium` |
| Private endpoint, AKS can't pull | DNS not resolving to private IP | Link private DNS zone `privatelink.azurecr.io` to AKS VNet |

---

## 19. Quick interview-style Q&A

**Q: What is ACR and why use it over Docker Hub?**
Private, Azure-integrated, identity-based access, close to Azure compute, Tasks, geo-replication, scanning, no public rate limits.

**Q: Difference between repository, image, tag, and digest?**
Repository = named bucket. Image = one build. Tag = mutable label. Digest = immutable SHA256.

**Q: How do AKS pods pull from ACR without secrets?**
Attach ACR to AKS → cluster's Managed Identity gets `AcrPull`. No `imagePullSecrets` needed.

**Q: What's the difference between admin user and Managed Identity?**
Admin user = static credential, shared, bad practice. Managed Identity = Entra-issued, rotating, scoped, auditable.

**Q: How do you build images without local Docker?**
ACR Tasks (`az acr build` or `az acr task create`) builds inside Azure from your source or a Git repo.

**Q: How would you secure ACR for production?**
Premium SKU + Private Endpoint, disable public access, disable admin user, RBAC via Managed Identity, retention policies, Defender for Containers, immutable production tags, OIDC-based CI/CD.

**Q: What does geo-replication do?**
One registry, replicas in multiple regions. Closest replica auto-served. Reduces latency and survives regional outages.

**Q: Pull by tag vs by digest — which in prod?**
**Digest**. Tags are mutable; digests are immutable. Guarantees the exact bits you deployed.

**Q: How do you keep base images patched automatically?**
ACR **base image update tasks** — when `mcr.microsoft.com/...` updates, your image rebuilds automatically.

---

## 20. Mental model (one-liner)

> **ACR = a private, Azure-native Docker registry where you push your images, pull them securely from Azure compute via Managed Identity, optionally build them in the cloud with Tasks, scan them with Defender, and replicate them globally on Premium.**
