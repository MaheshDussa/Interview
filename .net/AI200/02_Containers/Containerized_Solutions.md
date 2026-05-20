# Containerized Solutions on Azure (AZ-204)

> **One-liner**: Containers are portable app packages — Azure runs them on **ACI** (sandbox), **ACA** (serverless microservices), **AKS** (full Kubernetes), or **App Service** (PaaS).

> See companion notes: [Containers_And_Images.md](Containers_And_Images.md), [AzureContainerRegistry.md](AzureContainerRegistry.md), [Building_Images_Without_Docker.md](Building_Images_Without_Docker.md).

---

## 1. The Container Ecosystem on Azure

| Service | What it is | Best for |
|---|---|---|
| **Azure Container Registry (ACR)** | Private Docker/OCI registry | Storing images |
| **Azure Container Instances (ACI)** | Single container, fast spin-up | One-off jobs, dev/test |
| **Azure Container Apps (ACA)** | Serverless containers + Dapr + KEDA | Microservices, APIs, event-driven workers |
| **Azure Kubernetes Service (AKS)** | Managed Kubernetes | Full K8s control, complex workloads |
| **App Service (containers)** | PaaS web app from container | Single web app, easy custom domains |
| **Functions (containers)** | Serverless functions in custom image | Custom runtime/dependencies |

**Memory hint**: ACI = "container in a box"; ACA = "serverless K8s without the YAML"; AKS = "real K8s"; App Service = "web app that happens to be containerized".

---

## 2. Building an Image (.NET 9 multi-stage)

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# Runtime stage (smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

**Without Docker**: `dotnet publish -t:PublishContainer -p:ContainerRegistry=myacr.azurecr.io`.

---

## 3. Push to ACR

```powershell
az acr login --name myacr
docker tag myapi:dev myacr.azurecr.io/myapi:v1
docker push myacr.azurecr.io/myapi:v1

# Or build remotely
az acr build --registry myacr -t myapi:v1 .
```

---

## 4. Azure Container Instances (ACI)

- Single container or container **group** (multiple containers sharing lifecycle/network/storage — like a K8s pod).
- Per-second billing.
- No orchestration, no scale-out logic.

```powershell
az container create -g rg1 -n web1 \
  --image myacr.azurecr.io/myapi:v1 \
  --cpu 1 --memory 1 \
  --ports 80 --ip-address Public \
  --registry-login-server myacr.azurecr.io \
  --acr-identity [system] \
  --assign-identity
```

> Use for: scheduled batch jobs, build agents, demos, "run this container now".

---

## 5. Azure Container Apps (ACA) — preferred for microservices

Hides Kubernetes. Adds:

- **KEDA-based scaling** (HTTP, queue length, CPU, custom).
- **Scale to zero**.
- **Dapr** sidecar (state, pub/sub, secrets, service invocation).
- **Revisions** — multiple versions, traffic splitting (blue/green, canary).
- **Ingress** — internal or external HTTPS with auto cert.
- **Managed Identity** + Key Vault integration.

```powershell
az containerapp env create -g rg1 -n env1 -l eastus
az containerapp create -g rg1 -n api1 \
  --environment env1 \
  --image myacr.azurecr.io/myapi:v1 \
  --target-port 8080 --ingress external \
  --min-replicas 0 --max-replicas 10 \
  --registry-server myacr.azurecr.io \
  --system-assigned

# Traffic split between revisions (canary)
az containerapp revision set-mode -g rg1 -n api1 --mode multiple
az containerapp ingress traffic set -g rg1 -n api1 \
  --revision-weight api1--v1=80 api1--v2=20
```

### ACA scale rules
```json
"scale": {
  "minReplicas": 0,
  "maxReplicas": 30,
  "rules": [
    { "name": "http", "http": { "metadata": { "concurrentRequests": "50" } } },
    { "name": "queue", "custom": { "type": "azure-servicebus", "metadata": { "queueName":"orders", "messageCount":"5" } } }
  ]
}
```

---

## 6. Azure Kubernetes Service (AKS)

- Managed K8s control plane (free).
- You pay for **nodes** (VMs in node pools).
- Choose **system node pool** + **user node pools** (Linux/Windows).

```powershell
az aks create -g rg1 -n aks1 --node-count 3 \
  --enable-managed-identity --attach-acr myacr \
  --network-plugin azure --enable-addons monitoring \
  --node-vm-size Standard_D2s_v5

az aks get-credentials -g rg1 -n aks1
kubectl get nodes
```

### Key AKS concepts
| | |
|---|---|
| **Pod** | Smallest deployable unit (1+ containers) |
| **Deployment** | Manages ReplicaSets/pods |
| **Service** | Stable IP/DNS for pods |
| **Ingress** | HTTP routing (Application Gateway Ingress Controller, NGINX) |
| **HPA** | Horizontal Pod Autoscaler (CPU/memory) |
| **KEDA** | Event-driven autoscaling (queue, etc.) |
| **Workload Identity** | Pods auth to Azure via Entra (replaces pod-managed identity) |

### Sample manifest
```yaml
apiVersion: apps/v1
kind: Deployment
metadata: { name: api }
spec:
  replicas: 3
  selector: { matchLabels: { app: api } }
  template:
    metadata: { labels: { app: api } }
    spec:
      containers:
      - name: api
        image: myacr.azurecr.io/myapi:v1
        ports: [{ containerPort: 8080 }]
        readinessProbe: { httpGet: { path: /health, port: 8080 } }
---
apiVersion: v1
kind: Service
metadata: { name: api-svc }
spec:
  type: ClusterIP
  selector: { app: api }
  ports: [{ port: 80, targetPort: 8080 }]
```

---

## 7. ACA vs AKS vs ACI vs App Service

| Criteria | ACI | App Service | ACA | AKS |
|---|---|---|---|---|
| Setup effort | Lowest | Low | Low | Highest |
| Orchestration | None | Limited | Built-in (KEDA, Dapr) | Full K8s |
| Scale to zero | Yes (but pay/sec) | No | **Yes** | With KEDA |
| Microservices | Poor | Single app focus | **Great** | Great |
| GPU / custom node | Limited | No | No (some support) | **Yes** |
| Per-pod control | Limited | No | Limited | **Full** |

> Default recommendation for new microservices = **ACA**. Use AKS only if you need full K8s features.

---

## 8. Networking

- **ACA**: Inside an **environment** (workload profile or consumption). Internal or external ingress. Optional VNet injection.
- **AKS**: **Azure CNI** (pods get VNet IPs) or **kubenet** (NAT). Use **Private cluster** to hide API server.
- **ACI**: Public IP or VNet-deployed (subnet delegated to ACI).
- **App Service containers**: VNet Integration (out), Private Endpoint (in).

---

## 9. Auth & Pulling from ACR

| Service | How |
|---|---|
| App Service / ACA / ACI | **Managed Identity** + `AcrPull` role |
| AKS | `az aks update --attach-acr myacr` |
| Functions | Managed Identity + `AcrPull` |
| GitHub Actions / DevOps | OIDC federated credential (no secrets) |

---

## 10. Health, Probes, Graceful Shutdown

- ASP.NET Core: `app.MapHealthChecks("/health")`.
- K8s: `livenessProbe`, `readinessProbe`, `startupProbe`.
- ACA: ingress probes (HTTP/TCP) configurable per app.
- Handle `SIGTERM` — close DB connections, finish in-flight requests.

---

## 11. Storage

| Need | Option |
|---|---|
| Temp scratch | Container filesystem (ephemeral) |
| Shared volume in pod/group | EmptyDir / volume |
| Persistent across restarts | **Azure Files** (SMB) volume |
| High-perf disk | **Azure Disks** (RWO) |
| Object store | Blob (via SDK in code) |

---

## 12. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Image won't pull | MI not assigned `AcrPull`, or wrong tag |
| Container restarts | Missing env vars, port mismatch, OOM kill |
| Slow cold start | Use ACA "min replicas ≥ 1" or App Service Always On |
| Latest tag bites you | Pin to **digest** or immutable version tag |
| Root user in image | Add `USER` line, use non-root |
| Build OOM in CI | Use `az acr build` (cloud-side build) |
| Massive image size | Multi-stage build + chiseled/Alpine base |

---

## 13. AZ-204 Q&A

**Q1. When to use ACI vs ACA?**
ACI = single short-lived container, no scaling logic. ACA = serverless microservices with scale-to-zero, ingress, Dapr.

**Q2. What is a container group?**
Multiple containers sharing lifetime, network, and storage in ACI (like a K8s pod).

**Q3. How does ACA scale?**
KEDA scalers — HTTP RPS, queue length, CPU, custom metrics. Can scale to zero.

**Q4. How to pull from ACR without storing credentials?**
Enable **Managed Identity** on the consumer, grant `AcrPull` role on the registry.

**Q5. ACA revisions?**
Snapshots of an app version. Multiple-revision mode lets you split traffic for blue-green/canary.

**Q6. How to add a database to AKS?**
Don't run state in AKS — use **managed Azure SQL/Cosmos** + connect via VNet/Private Endpoint.

**Q7. ACI billing model?**
Per **vCPU-second + GB-second** based on resources × runtime.

**Q8. What is Dapr in ACA?**
Sidecar runtime providing building blocks: pub/sub, state, secrets, service invocation — vendor-neutral.

**Q9. How to expose AKS app to internet over HTTPS with WAF?**
Use **Application Gateway Ingress Controller (AGIC)** with WAF SKU, or **Front Door + Private Link**.

**Q10. How to update a container image with zero downtime in ACA?**
New image push → ACA creates new revision → ingress shifts traffic gradually if multi-revision mode + traffic split.

---

## 14. CLI Cheat Sheet

```powershell
# ACI
az container create -g rg1 -n job1 --image myacr.azurecr.io/job:v1 --restart-policy Never

# ACA
az containerapp up -n api1 -g rg1 --source . --ingress external --target-port 8080

# AKS
az aks create -g rg1 -n aks1 --enable-managed-identity --attach-acr myacr
az aks get-credentials -g rg1 -n aks1
kubectl apply -f deploy.yaml

# App Service (container)
az webapp create -g rg1 -p plan1 -n web1 \
  --deployment-container-image-name myacr.azurecr.io/web:v1
```

---

## 15. Mental Model

> **Pick the runtime by orchestration need: none → ACI; microservice with autoscale → ACA; full Kubernetes control → AKS; classic web app → App Service. Always pull from ACR via Managed Identity, pin image tags, run as non-root.**
