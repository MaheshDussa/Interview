# Building Container Images **Without Docker**

> **Short answer: yes, absolutely.** Docker is just one way to produce an **OCI image**. The image format is an open standard (OCI Image Spec), so many tools — and even .NET itself — can build images without Docker Desktop, without `dockerd`, and sometimes without a Dockerfile.

---

## 1. The key insight

A "Docker image" is really an **OCI Image**:
- A bunch of **tarball layers**
- A **manifest** (JSON) describing them
- A **config** (JSON) with entrypoint, env vars, etc.

Any tool that can produce that bundle and push it via the **OCI Distribution Spec** can talk to **ACR / Docker Hub / GHCR** — Docker not required.

---

## 2. Why would you avoid Docker?

| Reason | Explanation |
|---|---|
| **License** | Docker Desktop requires a paid license for larger companies |
| **Rootless / security** | `dockerd` runs as root; some hosts don't allow it |
| **CI agents** | Can't or shouldn't run Docker-in-Docker |
| **Speed** | Skip the daemon overhead; some tools build faster |
| **Reproducibility** | Tools like Buildah/Kaniko produce deterministic layers |
| **No local install** | Cloud-side builds (ACR Tasks) need nothing on your machine |
| **Languages tooling** | .NET, Java, Go can build images directly from project files |

---

## 3. The options at a glance

| Tool | Needs daemon? | Needs Dockerfile? | Where it runs |
|---|---|---|---|
| **.NET SDK** (`dotnet publish /t:PublishContainer`) | ❌ | ❌ | Locally / CI |
| **Buildah** | ❌ | ✅ (optional) | Linux |
| **Podman** | ❌ | ✅ | Linux / Windows / macOS |
| **Kaniko** | ❌ | ✅ | Inside a Kubernetes pod / CI container |
| **BuildKit** (`buildctl`) | ❌ (rootless mode) | ✅ | Linux, CI |
| **img** | ❌ | ✅ | Linux, rootless |
| **Jib** (Java) | ❌ | ❌ | Maven / Gradle |
| **ko** (Go) | ❌ | ❌ | CLI |
| **Bazel rules_oci** | ❌ | ❌ | Bazel builds |
| **ACR Tasks** (`az acr build`) | ❌ (cloud-side) | ✅ | Azure |
| **GitHub Actions cloud builders** | ❌ (cloud-side) | ✅ | GitHub |
| **nerdctl** + containerd | ❌ (uses containerd directly) | ✅ | Linux |

---

## 4. The easiest .NET way — built into the SDK (no Docker!)

.NET 7+ ships an **OCI image builder** in the SDK. **No Dockerfile, no Docker daemon required.**

### Build an image from a project
```powershell
# In your project folder
dotnet publish --os linux --arch x64 /t:PublishContainer `
  -p:ContainerRepository=orders-api `
  -p:ContainerImageTag=1.0
```

That's it — you get a local OCI image named `orders-api:1.0`.

### Customize via csproj
```xml
<PropertyGroup>
  <PublishProfile>DefaultContainer</PublishProfile>
  <ContainerBaseImage>mcr.microsoft.com/dotnet/aspnet:9.0</ContainerBaseImage>
  <ContainerRepository>orders-api</ContainerRepository>
  <ContainerImageTags>1.0;latest</ContainerImageTags>
  <ContainerRegistry>myregistry123.azurecr.io</ContainerRegistry>
  <ContainerUser>$APP_UID</ContainerUser>
  <ContainerPort Include="8080" />
</PropertyGroup>
```

### Push directly to ACR (no docker push)
```powershell
# Auth ACR first
az acr login -n myregistry123

dotnet publish /t:PublishContainer `
  -p:ContainerRegistry=myregistry123.azurecr.io `
  -p:ContainerRepository=orders-api `
  -p:ContainerImageTag=1.0
```

**Behind the scenes**: the SDK fetches the base image's layers, adds your published output as a new layer, builds an OCI manifest, and pushes everything via the registry's HTTP API. **Zero Docker.**

---

## 5. Cloud-side builds — ACR Tasks (zero local tools)

Already covered in the ACR notes. Re-summarized:

```powershell
# One-off build inside Azure, no local Docker needed
az acr build `
  -r myregistry123 `
  -t orders-api:{{.Run.ID}} `
  .
```

Use cases:
- ARM developers building amd64 images
- Locked-down corporate laptops
- CI agents without Docker

---

## 6. Buildah — pure Linux, no daemon

The "rootless Docker replacement" from the Podman project.

```bash
# Build from a Containerfile (same syntax as Dockerfile)
buildah bud -t orders-api:1.0 .

# Or imperatively, no file at all:
ctr=$(buildah from mcr.microsoft.com/dotnet/aspnet:9.0)
buildah copy   $ctr ./publish /app
buildah config --workingdir /app --entrypoint '["dotnet","MyApi.dll"]' $ctr
buildah commit $ctr orders-api:1.0

# Push to ACR
buildah push orders-api:1.0 docker://myregistry123.azurecr.io/orders-api:1.0
```

- Builds without a daemon
- Can run rootless
- 100% OCI-compliant

---

## 7. Podman — drop-in Docker CLI replacement

```bash
podman build -t orders-api:1.0 .
podman push  orders-api:1.0 myregistry123.azurecr.io/orders-api:1.0
```

- Same Dockerfile, same commands (`podman` is `docker` API-compatible)
- No daemon — each command is a normal process
- Available on Windows, macOS, Linux
- Pairs with `buildah` for advanced building

---

## 8. Kaniko — build inside Kubernetes / CI containers

Built by Google, runs **inside an unprivileged container**. Common in GitLab CI, Tekton, Argo Workflows.

```yaml
# K8s pod that builds + pushes
apiVersion: v1
kind: Pod
metadata: { name: kaniko }
spec:
  containers:
  - name: kaniko
    image: gcr.io/kaniko-project/executor:latest
    args:
    - --dockerfile=Dockerfile
    - --context=git://github.com/me/orders-api.git
    - --destination=myregistry123.azurecr.io/orders-api:1.0
    volumeMounts:
    - { name: docker-config, mountPath: /kaniko/.docker/ }
  volumes:
  - name: docker-config
    secret: { secretName: acr-auth }
```

Perfect when you can't run Docker-in-Docker on your build agents.

---

## 9. BuildKit (standalone) — Docker's engine without Docker

`buildkitd` + `buildctl` are the modern engine that powers `docker build`, but you can run them **standalone**, including **rootless**.

```bash
buildctl build \
  --frontend dockerfile.v0 \
  --local context=. \
  --local dockerfile=. \
  --output type=image,name=myregistry123.azurecr.io/orders-api:1.0,push=true
```

Fast, cacheable, multi-platform builds — without `dockerd`.

---

## 10. Jib (Java) and ko (Go) — language-native, no Dockerfile

### Jib (Java/Maven/Gradle)
```bash
mvn compile jib:build -Dimage=myregistry123.azurecr.io/orders-api:1.0
```
Builds an image directly from compiled classes. No Dockerfile. No Docker.

### ko (Go)
```bash
KO_DOCKER_REPO=myregistry123.azurecr.io ko build ./cmd/server
```
Builds, packages, and pushes a Go binary as an image — in seconds.

> .NET's `PublishContainer` is the direct counterpart to Jib/ko.

---

## 11. nerdctl + containerd — the "Docker without Docker" combo

[nerdctl](https://github.com/containerd/nerdctl) is a Docker-compatible CLI for **containerd** (the runtime Kubernetes uses). If you already have containerd installed:

```bash
nerdctl build -t orders-api:1.0 .
nerdctl push  orders-api:1.0 myregistry123.azurecr.io/orders-api:1.0
```

You get Docker UX with no Docker daemon — just containerd's BuildKit.

---

## 12. Build OCI images from a tarball (the manual way)

For ultimate control / learning, you can build an image with **just `tar` + JSON + an OCI client**:

1. Create a tar of your filesystem changes → **layer.tar.gz**.
2. Create a **config.json** with entrypoint, env, etc.
3. Create a **manifest.json** referencing the layer + config.
4. Push using a tool like [`oras`](https://oras.land/) or [`crane`](https://github.com/google/go-containerregistry/tree/main/cmd/crane).

```bash
crane append \
  --base mcr.microsoft.com/dotnet/aspnet:9.0 \
  --new_layer ./publish.tar.gz \
  --new_tag myregistry123.azurecr.io/orders-api:1.0
```

`crane` is excellent for **rebasing**, **copying**, and **modifying** images without rebuilding from source — no Docker needed.

---

## 13. CI pipelines — typical "no Docker" choices

| CI | Recommended builder |
|---|---|
| **GitHub Actions** | `docker/build-push-action` (has built-in BuildKit) **or** `dotnet publish /t:PublishContainer` **or** `az acr build` |
| **Azure DevOps** | `AzureCLI@2` → `az acr build` (cloud-side) |
| **GitLab CI** | Kaniko in a runner pod |
| **Tekton / Argo** | Kaniko or BuildKit tasks |
| **Jenkins** | Podman, Kaniko, or BuildKit agents |

---

## 14. Pulling/running without Docker too

Once the image exists, you can pull and run it without Docker:

- **Podman**: `podman run myregistry123.azurecr.io/orders-api:1.0`
- **containerd + nerdctl**: `nerdctl run ...`
- **Kubernetes** (uses containerd or CRI-O under the hood)
- **Azure Container Apps / App Service / AKS** — they pull straight from ACR; no Docker on your laptop ever.

---

## 15. Decision guide — "which should I use?"

```
Are you on .NET?
├─ Yes → `dotnet publish /t:PublishContainer`           ← easiest, no Dockerfile
└─ No
   ├─ Want zero local install? → `az acr build` (cloud)
   ├─ On Kubernetes CI?         → Kaniko
   ├─ Need Docker CLI muscle memory, rootless? → Podman
   ├─ Need advanced caching?    → BuildKit standalone
   ├─ Java/Go?                  → Jib / ko
   └─ Rebasing existing images? → crane
```

---

## 16. Quick "without Docker" examples for the same .NET app

### A) Pure .NET SDK
```powershell
dotnet publish /t:PublishContainer `
  -p:ContainerRegistry=myregistry123.azurecr.io `
  -p:ContainerRepository=orders-api `
  -p:ContainerImageTag=1.0
```

### B) ACR Tasks (cloud)
```powershell
az acr build -r myregistry123 -t orders-api:1.0 .
```

### C) Podman
```bash
podman build -t myregistry123.azurecr.io/orders-api:1.0 .
podman push   myregistry123.azurecr.io/orders-api:1.0
```

### D) Kaniko (in a pod)
```bash
/kaniko/executor \
  --dockerfile=Dockerfile \
  --context=. \
  --destination=myregistry123.azurecr.io/orders-api:1.0
```

All four produce the **same OCI image** that AKS / App Service / Container Apps will happily run.

---

## 17. Common gotchas

| Gotcha | Note |
|---|---|
| Multi-arch (arm64 + amd64) | Use BuildKit / Podman with `--platform`, or `dotnet publish --arch arm64` |
| Authentication | Tools rely on `~/.docker/config.json` or `~/.config/containers/auth.json`; `az acr login` writes the right entry |
| Rootless networking | Some tools (Kaniko) can't run privileged ops — keep Dockerfiles simple |
| `.dockerignore` | Most tools honor it; `dotnet publish` doesn't need it |
| Reproducibility | BuildKit / Jib / ko / .NET SDK produce deterministic layers; classic `docker build` doesn't |
| Layer caching | ACR Tasks, BuildKit, Kaniko support remote cache layers |

---

## 18. Interview-style Q&A

**Q: Do you need Docker to build a container image?**
No. Docker is just one builder. OCI images can be built by .NET SDK, Buildah, Podman, BuildKit, Kaniko, Jib, ko, ACR Tasks, etc.

**Q: How do you build a .NET image without a Dockerfile or Docker?**
`dotnet publish /t:PublishContainer` — the SDK builds and (optionally) pushes an OCI image straight to ACR.

**Q: Your CI runs in Kubernetes and can't use Docker-in-Docker. What now?**
Use **Kaniko** or **BuildKit** running in unprivileged pods, or push the build to **ACR Tasks**.

**Q: What's the difference between Podman and Docker?**
Podman is daemonless and rootless by default. CLI is Docker-compatible. Pairs with Buildah for advanced builds.

**Q: How does ACR accept images from non-Docker tools?**
ACR speaks the **OCI Distribution Spec**, the same HTTP API Docker uses. Any compliant client (Podman, Kaniko, crane, .NET SDK, oras…) can push to it.

**Q: Can you avoid Docker even at runtime?**
Yes — Kubernetes (containerd / CRI-O), Podman, Azure Container Apps, and App Service for Containers all run OCI images without Docker.

---

## 19. Mental model (one-liner)

> **Docker is one of many OCI-compliant tools. Container images are an open standard, so you can build them with .NET's SDK, Podman, BuildKit, Kaniko, Jib, ko, or even ACR itself — and ACR / AKS / App Service don't care which tool produced the image, only that it's OCI-compliant.**
