# 09 — Deploy: Docker, Azure & Production

> **One-liner**: Bake your Python app into a **slim, non-root Docker image** with pinned deps, then ship it to **Azure App Service / Container Apps / Functions / AKS**.

---

## 1. Production Checklist

- [ ] **Lockfile committed** (`uv.lock` / `poetry.lock` / pinned `requirements.txt`)
- [ ] **`.env` not in image** — use Azure App Settings / Key Vault
- [ ] **Non-root user** in container
- [ ] **Multi-stage Docker build** (small final image)
- [ ] **Health endpoint** (`/healthz` + `/ready`)
- [ ] **Structured logs** to stdout (no files)
- [ ] **Pinned base image** with digest
- [ ] **Vulnerability scan** in CI (Trivy / Defender)
- [ ] **Application Insights** wired
- [ ] **Worker count** appropriate (Gunicorn `-w`)

---

## 2. Production WSGI/ASGI Servers

| Server | Type | Notes |
|---|---|---|
| **Gunicorn** | WSGI/ASGI (with workers) | Linux only, battle-tested |
| **uWSGI** | WSGI | Legacy |
| **Uvicorn** | ASGI | FastAPI dev / behind Gunicorn for prod |
| **Hypercorn** | ASGI | HTTP/2, HTTP/3 |
| **Waitress** | WSGI | Pure-Python, **works on Windows** |
| **Granian** | ASGI | Rust-backed, very fast |

```powershell
# FastAPI (Linux container)
gunicorn myapi.main:app -k uvicorn.workers.UvicornWorker -w 4 -b 0.0.0.0:8000

# Flask (Linux)
gunicorn "myapi:create_app()" -w 4 -b 0.0.0.0:8000

# Flask (Windows)
waitress-serve --listen=0.0.0.0:8000 --call myapi:create_app
```

Worker rule of thumb: `(2 × CPU) + 1` for I/O-bound; 1 per core for CPU-bound.

---

## 3. Dockerfile (FastAPI, multi-stage, pip)

```dockerfile
# syntax=docker/dockerfile:1.7

# Build stage
FROM python:3.12-slim AS build
ENV PIP_DISABLE_PIP_VERSION_CHECK=1 PIP_NO_CACHE_DIR=1
WORKDIR /app
COPY requirements.txt .
RUN pip install --prefix=/install -r requirements.txt

# Runtime stage
FROM python:3.12-slim
ENV PYTHONUNBUFFERED=1 PYTHONDONTWRITEBYTECODE=1
WORKDIR /app

# non-root user
RUN useradd -m -u 10001 app && chown -R app /app

COPY --from=build /install /usr/local
COPY src /app/src

USER app
EXPOSE 8000
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD python -c "import urllib.request,sys; sys.exit(0 if urllib.request.urlopen('http://localhost:8000/healthz').status==200 else 1)"
CMD ["gunicorn", "myapi.main:app", "-k", "uvicorn.workers.UvicornWorker", \
     "-w", "4", "-b", "0.0.0.0:8000", "--access-logfile", "-"]
```

> `slim` ≈ 50 MB; full Python image ≈ 1 GB. Use `slim` (or distroless / chiseled) in prod.

---

## 4. Dockerfile with `uv` (fastest)

```dockerfile
FROM python:3.12-slim AS build
COPY --from=ghcr.io/astral-sh/uv:0.4.20 /uv /usr/local/bin/uv
WORKDIR /app
COPY pyproject.toml uv.lock ./
RUN uv sync --frozen --no-dev --no-install-project

COPY src ./src
RUN uv sync --frozen --no-dev

FROM python:3.12-slim
ENV PATH="/app/.venv/bin:$PATH" PYTHONUNBUFFERED=1
WORKDIR /app
RUN useradd -m -u 10001 app && chown -R app /app
COPY --from=build /app /app
USER app
EXPOSE 8000
CMD ["uvicorn", "myapi.main:app", "--host", "0.0.0.0", "--port", "8000"]
```

---

## 5. `.dockerignore`

```
.venv/
__pycache__/
*.pyc
.git/
.idea/ .vscode/
.env
tests/
docs/
*.md
.pytest_cache/
.mypy_cache/
.ruff_cache/
```

Smaller build context = faster builds.

---

## 6. Build & Run Locally

```powershell
docker build -t myapi:dev .
docker run --rm -p 8000:8000 -e LOG_LEVEL=INFO myapi:dev

# Multi-arch (linux/amd64 + arm64)
docker buildx build --platform linux/amd64,linux/arm64 -t myacr.azurecr.io/myapi:v1 --push .
```

---

## 7. Push to Azure Container Registry (ACR)

```powershell
az acr login --name myacr
docker tag myapi:dev myacr.azurecr.io/myapi:v1
docker push myacr.azurecr.io/myapi:v1

# OR build remotely (no local Docker)
az acr build --registry myacr -t myapi:v1 .
```

> See [02_Containers in AI200](../AI200/02_Containers/AzureContainerRegistry.md) for the full ACR notes.

---

## 8. Deploy to Azure Container Apps (recommended)

```powershell
az containerapp env create -g rg1 -n env1 -l eastus

az containerapp create -g rg1 -n myapi \
  --environment env1 \
  --image myacr.azurecr.io/myapi:v1 \
  --target-port 8000 --ingress external \
  --min-replicas 1 --max-replicas 10 \
  --registry-server myacr.azurecr.io \
  --system-assigned

# Grant the MI AcrPull
$mi = az containerapp show -g rg1 -n myapi --query identity.principalId -o tsv
az role assignment create --assignee $mi --role AcrPull \
  --scope $(az acr show -n myacr --query id -o tsv)

# Add settings + secrets
az containerapp update -g rg1 -n myapi \
  --set-env-vars LOG_LEVEL=INFO DB_URL=secretref:db-url
```

Pros: scale-to-zero, KEDA, revisions, traffic split, Dapr, VNet.

---

## 9. Deploy to App Service (single web app)

```powershell
az appservice plan create -g rg1 -n plan1 --sku B1 --is-linux
az webapp create -g rg1 -p plan1 -n myapi-web \
  --deployment-container-image-name myacr.azurecr.io/myapi:v1

az webapp identity assign -g rg1 -n myapi-web
$mi = az webapp show -g rg1 -n myapi-web --query identity.principalId -o tsv
az role assignment create --assignee $mi --role AcrPull \
  --scope $(az acr show -n myacr --query id -o tsv)

az webapp config appsettings set -g rg1 -n myapi-web --settings \
  WEBSITES_PORT=8000 LOG_LEVEL=INFO

az webapp restart -g rg1 -n myapi-web
```

Key setting: **`WEBSITES_PORT`** must match the container's port.

---

## 10. Deploy as Azure Functions (Python)

Functions runtime hosts your Python code event-driven (HTTP, Timer, Blob, Queue…).

```powershell
pip install azure-functions
func init myfn --python --model V2
cd myfn
func new --name HttpExample --template "HTTP trigger"
func start
```

`function_app.py` (model V2):
```python
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)

@app.route(route="hello")
def hello(req: func.HttpRequest) -> func.HttpResponse:
    return func.HttpResponse("Hello from Functions!", status_code=200)
```

Deploy:
```powershell
func azure functionapp publish myfuncapp --python
```

> See [Azure_Functions.md](../AI200/01_Compute/Azure_Functions.md) for plans, triggers, durable, etc.

---

## 11. Deploy to AKS (for full Kubernetes)

`Dockerfile` → ACR → manifest:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata: { name: myapi }
spec:
  replicas: 3
  selector: { matchLabels: { app: myapi } }
  template:
    metadata: { labels: { app: myapi } }
    spec:
      containers:
      - name: api
        image: myacr.azurecr.io/myapi:v1
        ports: [{ containerPort: 8000 }]
        envFrom:
        - secretRef: { name: myapi-env }
        readinessProbe: { httpGet: { path: /ready,    port: 8000 } }
        livenessProbe:  { httpGet: { path: /healthz, port: 8000 } }
        resources:
          requests: { cpu: "100m", memory: "128Mi" }
          limits:   { cpu: "500m", memory: "512Mi" }
---
apiVersion: v1
kind: Service
metadata: { name: myapi-svc }
spec:
  selector: { app: myapi }
  ports: [{ port: 80, targetPort: 8000 }]
  type: ClusterIP
```

```powershell
az aks get-credentials -g rg1 -n aks1
kubectl apply -f deploy.yaml
```

---

## 12. Secrets & Settings

| Where | Mechanism |
|---|---|
| Local dev | `.env` + `python-dotenv` / `pydantic-settings` |
| App Service / Functions | App Settings + **Key Vault references** (`@Microsoft.KeyVault(...)`) |
| Container Apps | Secrets + `secretref:` envs |
| AKS | Kubernetes Secrets / **CSI Secrets Store** with Key Vault |
| Any | **Managed Identity** + `DefaultAzureCredential` to fetch at runtime |

```python
# pip install azure-identity azure-keyvault-secrets
from azure.identity import DefaultAzureCredential
from azure.keyvault.secrets import SecretClient

vault = SecretClient("https://kv1.vault.azure.net", DefaultAzureCredential())
db_url = vault.get_secret("db-url").value
```

---

## 13. Logging in Production

- Write to **stdout/stderr** only (containers / App Service capture it).
- Use JSON logs for structured ingestion.

```python
import logging, json, sys

class JsonFmt(logging.Formatter):
    def format(self, r):
        return json.dumps({
            "ts": self.formatTime(r),
            "lvl": r.levelname,
            "name": r.name,
            "msg": r.getMessage(),
        })

h = logging.StreamHandler(sys.stdout)
h.setFormatter(JsonFmt())
logging.basicConfig(level=logging.INFO, handlers=[h])
```

---

## 14. Application Insights

```powershell
pip install opentelemetry-distro[azure] azure-monitor-opentelemetry
```

```python
from azure.monitor.opentelemetry import configure_azure_monitor

configure_azure_monitor(
    connection_string=os.environ["APPLICATIONINSIGHTS_CONNECTION_STRING"]
)
```

Auto-instruments FastAPI/Flask, requests, httpx, SQLAlchemy, logging.

---

## 15. Performance Knobs

| Knob | Effect |
|---|---|
| Worker count (`-w`) | Concurrent requests |
| `worker-class` (uvicorn vs sync) | Async vs sync |
| `--timeout` | Kill slow requests |
| `--max-requests` | Recycle workers (prevents memory leaks) |
| Async DB driver (`asyncpg`) | Higher throughput |
| Caching (Redis / `functools.cache`) | Cheap latency win |
| `uvloop` | Faster event loop on Linux |
| `orjson` / `ujson` | Faster JSON serialization |

---

## 16. Database Migrations in Deploy

Run migrations **as a separate step before swap**, not inside the web container start.

```powershell
# Container Apps: a job revision
az containerapp job create -g rg1 -n migrate \
  --image myacr.azurecr.io/myapi:v1 \
  --trigger-type Manual \
  --command "alembic upgrade head"

# AKS
kubectl run migrate --image=myacr.azurecr.io/myapi:v1 --restart=Never \
  --command -- alembic upgrade head
```

---

## 17. Zero-Downtime Deploys

| Platform | Mechanism |
|---|---|
| App Service | **Deployment slots** + swap |
| Container Apps | Revisions + traffic split |
| AKS | Rolling update / Argo Rollouts / Flagger |
| Functions | Slots (Premium / Dedicated) |

Always include **readiness probes** so traffic only goes to ready replicas.

---

## 18. CI/CD (GitHub Actions example)

```yaml
name: cd
on:
  push: { branches: [main] }

jobs:
  build-deploy:
    runs-on: ubuntu-latest
    permissions: { id-token: write, contents: read }
    steps:
      - uses: actions/checkout@v4
      - uses: azure/login@v2
        with: { client-id: ${{ secrets.AZURE_CLIENT_ID }}, tenant-id: ${{ secrets.AZURE_TENANT_ID }}, subscription-id: ${{ secrets.AZURE_SUB }} }
      - run: az acr build --registry myacr -t myapi:${{ github.sha }} .
      - run: |
          az containerapp update -g rg1 -n myapi \
            --image myacr.azurecr.io/myapi:${{ github.sha }}
```

Uses **OIDC federated credential** — no secrets stored in GitHub.

---

## 19. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Huge image (>1 GB) | Use `python:3.12-slim` + multi-stage |
| Slow cold start | Use Container Apps with min replicas ≥ 1 / App Service Always On |
| Container exits | Check `CMD` runs in foreground; Gunicorn binds `0.0.0.0` |
| `WEBSITES_PORT` mismatch | Set on App Service to match `EXPOSE` |
| Logs not visible | Write to stdout, not files |
| Secrets in image | Use App Settings / Key Vault |
| Migration during start | Causes race when scaled out; run as job |
| Crashes on first request | Lazy DB connect; verify `/ready` first |
| Wrong arch (M1 vs amd64) | `--platform linux/amd64` when building on Mac |

---

## 20. Interview Q&A

**Q1. Why multi-stage Docker build?**
Compile / install in fat image, copy only artifacts into slim runtime — smaller, faster, fewer CVEs.

**Q2. Run Python on Windows host in prod?**
Use Waitress (pure-Python WSGI). For ASGI/FastAPI containerize on Linux.

**Q3. App Service vs Container Apps vs AKS for Python?**
App Service for simple web apps. Container Apps for serverless microservices (scale-to-zero, KEDA). AKS for full K8s control.

**Q4. How to keep secrets out of code?**
Managed Identity + Key Vault references (App Service) / Key Vault SDK / Container Apps secrets.

**Q5. How to deploy without secrets in CI?**
Azure OIDC federated credential in GitHub Actions / Azure DevOps.

**Q6. What does `WEBSITES_PORT` do?**
Tells App Service which port your container listens on (default behavior expects 80/8080).

**Q7. How to do zero-downtime deploys?**
Slots + swap, or rolling update with readiness probes.

**Q8. Difference between WSGI and ASGI?**
WSGI is sync request-response. ASGI supports async, websockets, HTTP/2.

**Q9. Why pin Python and base image?**
Reproducible builds; avoid surprise breaking changes from upstream.

**Q10. Where do logs go in App Service / Container Apps?**
Anything on stdout/stderr is streamed; configure Diagnostic Settings → Log Analytics for retention/query.

---

## 21. Cheat Sheet

```powershell
# Build & push
az acr build --registry myacr -t myapi:v1 .

# Run locally
docker run --rm -p 8000:8000 myacr.azurecr.io/myapi:v1

# Container Apps deploy
az containerapp update -g rg1 -n myapi --image myacr.azurecr.io/myapi:v1

# App Service deploy
az webapp config container set -g rg1 -n myapi-web \
  --docker-custom-image-name myacr.azurecr.io/myapi:v1

# Functions deploy
func azure functionapp publish myfuncapp --python
```

---

## 22. Mental Model

> **Slim multi-stage image → push to ACR → Managed Identity pulls it → run on Container Apps (default) / App Service (single app) / AKS (full K8s) / Functions (event-driven). Secrets via Key Vault, logs via stdout, deploys via slots or revisions, observability via App Insights.**
