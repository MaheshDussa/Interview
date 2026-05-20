# Containers & Container Images — Self-Explanatory Notes

> The simplest, clearest mental model for what containers and images actually are, how they work, and how to use them in real life.

---

## 1. The 10-second definition

- **Container image** = a **read-only blueprint**: your app + its dependencies + OS files + config, packaged together.
- **Container**       = a **running instance** of that image — an isolated process with its own filesystem, network, and PID space.

> **Analogy:** image is a **recipe**; container is the **cooked dish** made from that recipe. One recipe → many dishes.

---

## 2. Why containers exist (the problem they solve)

"It works on my machine" — the classic dev pain.

Before containers, deploying an app meant:
- Install the right OS packages
- Install the right runtime version (.NET 6 / 8 / 9, Node 18 vs 20, etc.)
- Set environment variables
- Copy config files
- Hope production matches dev

A container **bundles all of that into one portable unit** that runs the same on a laptop, a build agent, AKS, EC2, or a Raspberry Pi.

| Problem | Container fix |
|---|---|
| Different runtime versions across envs | Runtime baked into image |
| Slow VM startup (minutes) | Containers start in milliseconds |
| Heavy VMs (full OS per app) | Containers share host kernel — tiny |
| Hard to scale | One image → spin up 100 identical copies |
| Dependency conflicts | Each container has its own filesystem |

---

## 3. Container vs Virtual Machine

| | Virtual Machine | Container |
|---|---|---|
| Isolation level | Full OS (kernel + user space) | Process-level (shared kernel) |
| Size | GBs | MBs |
| Startup | Minutes | Milliseconds |
| Density per host | 10s | 100s–1000s |
| Use case | Strong OS isolation, multi-OS | App packaging & scaling |

**Containers = lightweight process isolation, not virtual machines.**

---

## 4. What's inside a container image?

A container image is built from **layers**, each layer is a tarball of filesystem changes.

```
┌──────────────────────────────────────┐
│ Layer 4: your app DLLs / binaries    │  ← changes most often
├──────────────────────────────────────┤
│ Layer 3: NuGet / npm dependencies    │
├──────────────────────────────────────┤
│ Layer 2: .NET / Node runtime         │
├──────────────────────────────────────┤
│ Layer 1: base OS (Alpine / Ubuntu)   │  ← changes rarely
└──────────────────────────────────────┘
```

Plus **metadata** (the **manifest**):
- Entry point (`dotnet App.dll`)
- Working directory
- Environment variables
- Exposed ports
- Default user
- Labels (version, owner, etc.)

**Why layers matter:**
- **Cached & reused** — pulling a new image only downloads layers you don't already have.
- **Shared across images** — two images using `dotnet/aspnet:9.0` share that layer on disk.
- **Reproducible** — each layer has a SHA256 hash.

---

## 5. Image identity: tag vs digest

```
mcr.microsoft.com/dotnet/aspnet:9.0
└────────┬────────┘ └────┬────┘ └┬┘
       registry      repository  tag
```

- **Tag** (`9.0`, `latest`, `v1.2.3`) — *mutable* label. Today's `:latest` may be different tomorrow.
- **Digest** (`@sha256:abc123…`) — *immutable* fingerprint. Always the same exact bits.

> **Production rule:** deploy by digest, not by tag.

```
mcr.microsoft.com/dotnet/aspnet@sha256:9f3c…
```

---

## 6. How a container actually runs (under the hood)

A container is **just a Linux process** with extra restrictions:

| Linux feature | What it provides |
|---|---|
| **Namespaces** | Each container thinks it has its own PID list, network, mounts, hostname, users |
| **cgroups** | Limit CPU, RAM, IO, PIDs |
| **chroot / overlayfs** | Each container sees its own filesystem (layered) |
| **Capabilities & seccomp** | Restrict what syscalls the process can make |

On Windows, equivalent kernel features (Job Objects, Silos, Server Containers, Hyper-V isolation).

> No "container engine kernel" exists — the **host kernel** runs everything; containers just see a sliced view of it.

---

## 7. Image lifecycle (the whole journey)

```
   Dockerfile  ── docker build ──▶  Image (local)
                                       │
                          docker tag   ▼
                                   tagged image
                                       │
                          docker push  ▼
                                   Registry (ACR / Docker Hub)
                                       │
                          docker pull  ▼
                                   Image on target host
                                       │
                          docker run   ▼
                                   Running container
                                       │
                          docker stop  ▼
                                   Stopped container (filesystem kept)
                                       │
                          docker rm    ▼
                                       gone
```

---

## 8. Dockerfile — the recipe

A Dockerfile is a text file describing **how to build an image**.

### Minimal ASP.NET Core example (multi-stage)

```dockerfile
# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore                # cached unless csproj changes
COPY . .
RUN dotnet publish -c Release -o /app /p:UseAppHost=false

# ---- runtime stage (small!) ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID                     # run as non-root (security)
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

**Why multi-stage?**
- `sdk` image is ~1 GB (has compilers).
- `aspnet` image is ~200 MB (just runtime).
- Final image contains only what's needed to RUN, not BUILD.

### Common Dockerfile instructions

| Instruction | What it does |
|---|---|
| `FROM` | Base image to start from |
| `WORKDIR` | Set current directory (creates if missing) |
| `COPY` / `ADD` | Copy files from build context into image |
| `RUN` | Run a command at **build** time (creates a layer) |
| `ENV` | Set environment variable |
| `EXPOSE` | Document a port (doesn't actually publish) |
| `USER` | Switch to non-root user |
| `ENTRYPOINT` | The command the container starts with |
| `CMD` | Default args for ENTRYPOINT |
| `HEALTHCHECK` | How Docker knows the app is alive |

---

## 9. Container vs Image — side-by-side

| | Image | Container |
|---|---|---|
| State | Read-only | Read-write (top layer is writable) |
| Lives on | Disk in a registry / host | A running host process |
| Created with | `docker build` | `docker run` |
| Multiple from same source | One image | Many containers |
| Persisted on stop | Yes | Filesystem yes, RAM no — until `rm` |
| Identity | name:tag / digest | Container ID + name |

---

## 10. Essential commands (Docker CLI)

```powershell
# Images
docker build -t my-api:1.0 .            # build from Dockerfile in current dir
docker images                           # list local images
docker pull mcr.microsoft.com/dotnet/aspnet:9.0
docker tag my-api:1.0 myacr.azurecr.io/my-api:1.0
docker push myacr.azurecr.io/my-api:1.0
docker rmi my-api:old                   # remove local image
docker history my-api:1.0               # see layers and sizes

# Containers
docker run -d -p 8080:8080 --name api my-api:1.0    # run detached
docker ps                                # running containers
docker ps -a                             # include stopped
docker logs -f api                       # follow logs
docker exec -it api sh                   # shell inside a running container
docker stop api && docker rm api         # stop + remove
docker inspect api                       # full JSON metadata
docker stats                             # live CPU/RAM

# Housekeeping
docker system df                         # disk usage
docker system prune -af                  # remove ALL unused images/containers (careful!)
```

---

## 11. Networking, basics

```powershell
# Map host port 8080 -> container port 80
docker run -p 8080:80 nginx
```

- `-p host:container` publishes the port.
- Containers on the same Docker network can reach each other by **container name** as a DNS hostname.
- Default network types: `bridge` (single host), `host` (no isolation), `overlay` (multi-host, Swarm/K8s).

---

## 12. Storage & data persistence

A container's filesystem **disappears when the container is removed**. For persistent data use:

| Option | Use case |
|---|---|
| **Volume** (`-v mydata:/data`) | Managed by Docker — preferred for DBs, app data |
| **Bind mount** (`-v C:\src:/src`) | Mount a host folder — great for dev hot-reload |
| **tmpfs** | In-memory only (secrets, scratch) |

```powershell
docker volume create pgdata
docker run -d -v pgdata:/var/lib/postgresql/data postgres:16
```

---

## 13. Environment variables & config

```powershell
docker run -e ConnectionStrings__Default="Server=..." my-api:1.0
docker run --env-file .env my-api:1.0
```

In ASP.NET Core: env vars with `:` replaced by `__` map to configuration paths.

**Never bake secrets into images.** Inject at runtime via env vars, mounted files, or a secrets manager (Key Vault).

---

## 14. Best practices for production images

1. **Use small base images** — `mcr.microsoft.com/dotnet/aspnet:9.0` (or `-alpine` / `-jammy-chiseled`).
2. **Multi-stage builds** — strip out SDK / compilers from the final image.
3. **Run as non-root** — `USER $APP_UID` (built into Microsoft .NET images).
4. **Pin versions** — `:9.0` not `:latest`; pin by digest in production.
5. **Order layers cache-friendly** — copy `csproj` and restore BEFORE copying source.
6. **Use `.dockerignore`** to skip `bin/`, `obj/`, `.git/`, `node_modules/`.
7. **One process per container** — let the orchestrator restart it if it crashes.
8. **HEALTHCHECK** — so orchestrators know when to restart unhealthy containers.
9. **Scan images** — Defender for Containers, Trivy, Snyk.
10. **Sign images** — content trust / cosign for supply-chain security.

### .dockerignore example
```
bin/
obj/
.git/
*.user
node_modules/
*.md
```

---

## 15. Container lifecycle states

```
        created ──run──▶ running ──stop──▶ exited
                            │
                          pause
                            ▼
                          paused ──unpause──▶ running
                            │
                          kill
                            ▼
                          exited ──rm──▶ (gone)
```

`exited` containers still occupy disk until `docker rm`.

---

## 16. Orchestrators — why & which?

A few containers on one host = OK with plain Docker.
Many containers across many hosts = you need an orchestrator.

| Tool | What it does |
|---|---|
| **Docker Compose** | Define multi-container app in `docker-compose.yml`. One host. Great for dev. |
| **Kubernetes (AKS / EKS / GKE)** | Industry standard. Scheduling, scaling, self-healing, networking, secrets, storage. |
| **Azure Container Apps** | Serverless containers on K8s under the hood. No cluster to manage. |
| **Azure App Service for Containers** | PaaS hosting for a single container app. |
| **Docker Swarm** | Built into Docker. Simple. Mostly legacy. |

### Tiny `docker-compose.yml`
```yaml
services:
  api:
    image: myacr.azurecr.io/my-api:1.0
    ports: [ "8080:8080" ]
    environment:
      ASPNETCORE_ENVIRONMENT: Production
    depends_on: [ db ]
  db:
    image: postgres:16
    volumes: [ "pgdata:/var/lib/postgresql/data" ]
    environment:
      POSTGRES_PASSWORD: example
volumes:
  pgdata:
```
Run: `docker compose up -d`.

---

## 17. Image vs Container — common confusions cleared

**"I changed the image — why isn't my container updated?"**
Containers are created from a **snapshot** of the image. Rebuild + recreate the container.

**"I `docker rm`'d my container — did I lose my data?"**
Yes, **unless** you used a volume or bind mount.

**"What's the difference between `EXPOSE` and `-p`?**
`EXPOSE` only documents intent in the image metadata. `-p host:container` actually publishes the port to the host.

**"Why is `:latest` dangerous?"**
It's a moving target. Today's `latest` is tomorrow's something else — no reproducibility.

**"Is a container a tiny VM?"**
No. A container is a process with isolated namespaces and cgroups. It shares the host kernel.

---

## 18. OCI standards (the boring but important bit)

What we call "Docker images" are really **OCI Images**:

| Spec | What it standardizes |
|---|---|
| **OCI Image Spec** | Layer format, manifest, config |
| **OCI Distribution Spec** | How registries (ACR, Docker Hub, GHCR) serve images over HTTP |
| **OCI Runtime Spec** | How runtimes (runc, containerd, CRI-O) execute containers |

Because of OCI, ACR works with Podman, Kubernetes, BuildKit, containerd — not just Docker.

---

## 19. Interview-style Q&A

**Q: What is a container image?**
An immutable, layered filesystem + metadata describing how to run an app. Built from a Dockerfile, stored in a registry.

**Q: What is a container?**
A running instance of an image — a process with isolated namespaces, cgroups, and its own writable layer.

**Q: Image vs Container in one line?**
Image = class, Container = instance.

**Q: Why use multi-stage Dockerfiles?**
To exclude build tools (SDK, compilers) from the final runtime image — smaller, faster, more secure.

**Q: How do containers differ from VMs?**
Containers share the host kernel and isolate via namespaces/cgroups. VMs virtualize hardware and run a full guest OS. Containers are smaller and start faster.

**Q: Where do containers store data?**
By default in the writable top layer (lost on `rm`). For persistence, mount volumes or bind mounts.

**Q: What is `latest` and why avoid it in production?**
A default mutable tag. Pulling `:latest` tomorrow may give a different image. Pin a version or digest.

**Q: How does Docker reuse layers?**
Each Dockerfile instruction creates a layer with a hash. Identical layers are stored once and reused across images.

**Q: How do you make a container start a healthy app reliably?**
- Run as non-root, use a small base image
- `HEALTHCHECK` directive
- Orchestrator probes (`liveness` / `readiness` in K8s)
- Graceful shutdown (handle `SIGTERM`)

**Q: How are containers isolated?**
Linux namespaces (PID, NET, MNT, UTS, IPC, USER), cgroups, capabilities, seccomp, AppArmor/SELinux. On Windows, equivalent kernel objects.

---

## 20. Mental model in one sentence

> **An image is a layered, immutable blueprint of an app + its dependencies; a container is a lightweight, isolated process that runs that blueprint — sharing the host's kernel but seeing its own private filesystem, network, and process tree.**
