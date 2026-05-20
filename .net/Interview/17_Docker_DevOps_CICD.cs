// =====================================================================
//  17) DOCKER, KUBERNETES, GIT, CI/CD — Interview Q&A
// =====================================================================
namespace Interview.DevOps
{
    // =====================================================================
    //  DOCKER
    // =====================================================================

    // Q1: What is Docker / a container?
    // A : Container = lightweight isolated process using the HOST kernel
    //     plus a layered filesystem from an image. Image = read-only
    //     template (built from a Dockerfile).

    // Q2: VM vs Container?
    // A : VM  - own OS kernel, GBs, minutes to boot, strong isolation.
    //     Ctn - share kernel, MBs, seconds to start, lighter isolation.

    // Q3: Multi-stage Dockerfile for ASP.NET Core (interview classic):
    //
    //   /// # build stage
    //   /// FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    //   /// WORKDIR /src
    //   /// COPY *.csproj ./
    //   /// RUN dotnet restore
    //   /// COPY . .
    //   /// RUN dotnet publish -c Release -o /app /p:UseAppHost=false
    //   ///
    //   /// # runtime stage (small)
    //   /// FROM mcr.microsoft.com/dotnet/aspnet:8.0
    //   /// WORKDIR /app
    //   /// COPY --from=build /app .
    //   /// EXPOSE 8080
    //   /// ENTRYPOINT ["dotnet", "MyApp.dll"]
    //
    // Why multi-stage? Final image excludes SDK + source -> smaller, safer.

    // Q4: Common Docker commands.
    //   docker build -t myapp:1.0 .
    //   docker run  -p 8080:8080 --name myapp -e ASPNETCORE_ENVIRONMENT=Production myapp:1.0
    //   docker ps / docker logs / docker exec -it myapp bash
    //   docker compose up -d

    // Q5: Image layers / cache?
    // A : Each Dockerfile instruction creates a layer. Reuse layers by
    //     ordering: copy csproj + restore BEFORE copying source. Changes to
    //     code won't invalidate the restore layer.

    // Q6: .dockerignore?
    // A : Like .gitignore for builds. Exclude bin/, obj/, .git/, secrets;
    //     speeds builds and avoids leaking files into the image.

    // Q7: Multi-arch images?
    // A : `docker buildx build --platform linux/amd64,linux/arm64 -t img:tag .`
    //     One tag, multiple architectures. Important for ARM (M1/Mac, Graviton).

    // Q8: Distroless / Chiseled images (.NET 8+)?
    // A : Minimal base images (no shell, no apt). Smaller, fewer CVEs.
    //     Microsoft publishes "chiseled" Ubuntu images for .NET.

    // Q9: Container secrets — anti-patterns?
    // A : Don't bake secrets into images, don't print them in logs, don't
    //     pass via plain env vars in compose committed to git. Use:
    //     Docker secrets, K8s Secret + CSI driver, cloud KeyVault.

    // =====================================================================
    //  KUBERNETES (high level for .NET interviews)
    // =====================================================================

    // Q10: Pod vs Deployment vs ReplicaSet vs Service vs Ingress?
    // A : Pod        - smallest unit; one or more containers sharing net/storage.
    //     ReplicaSet - ensures N pod replicas.
    //     Deployment - declarative ReplicaSet + rollout/rollback.
    //     Service    - stable virtual IP / DNS in front of pods (load balanced).
    //     Ingress    - HTTP routing (host/path) to Services; needs a controller
    //                  (NGINX, AGIC for App Gateway).

    // Q11: ConfigMap vs Secret?
    // A : ConfigMap - non-sensitive config (key/value).
    //     Secret    - base64-encoded sensitive data; can integrate with KeyVault.
    //     Both can be mounted as files or env vars.

    // Q12: Probes — liveness vs readiness vs startup?
    // A : Startup    - "still booting?" pauses other probes until OK.
    //     Liveness   - "is it alive?" failed = restart.
    //     Readiness  - "ready to serve traffic?" failed = remove from Service.
    //     Map them to /health (live) and /ready (incl. dependency checks).

    // Q13: HPA / Cluster Autoscaler / KEDA?
    // A : HPA  - scale pods on CPU/mem/custom metrics.
    //     CA   - add/remove nodes when pods can't schedule.
    //     KEDA - event-driven (queue length, Kafka lag, HTTP RPS).

    // Q14: Rolling update vs Recreate?
    // A : Rolling - replace pods gradually (default; zero downtime if probes work).
    //     Recreate - kill all then start; brief downtime.

    // Q15: Resource requests vs limits.
    // A : requests - scheduler reserves at least this much.
    //     limits   - hard cap; CPU throttled, memory OOMKilled if exceeded.
    //     Always set requests; limits prevent noisy neighbors.

    // Q16: NetworkPolicies?
    // A : Allow/deny pod-to-pod traffic by label selectors. Default-deny is
    //     a security best practice.

    // Q17: Helm vs raw manifests vs Kustomize?
    // A : Helm      - templated charts + values per env; package manager feel.
    //     Kustomize - overlays without templating; built into kubectl.
    //     Raw YAML  - simple, but copy-paste across envs.

    // =====================================================================
    //  GIT (the basics interviewers actually ask)
    // =====================================================================

    // Q18: git fetch vs pull vs rebase vs merge?
    // A : fetch  - download refs, don't change working branch.
    //     pull   - fetch + merge (or pull --rebase).
    //     merge  - keeps history with a merge commit.
    //     rebase - rewrites commits on top of another branch (linear history).
    //     Don't rebase shared/published branches.

    // Q19: Common workflows.
    // A : - Git Flow: main, develop, feature/*, release/*, hotfix/*.
    //     - Trunk-Based: short-lived feature branches into main, feature flags.
    //     - GitHub Flow: branch -> PR -> review -> merge.

    // Q20: How to recover after bad commit?
    // A : - git revert <sha>   (safe, public; adds a revert commit)
    //     - git reset --soft   (keep changes staged)
    //     - git reset --hard   (destructive; only on local)
    //     - git reflog         (find lost commits)

    // Q21: Resolve a merge conflict?
    //   git fetch origin
    //   git merge origin/main   # or rebase
    //   # fix conflicts in files (look for <<<<<<<)
    //   git add <files>
    //   git commit              # or git rebase --continue

    // Q22: .gitignore essentials for .NET?
    //   bin/  obj/  *.user  .vs/  appsettings.Local.json  *.db  .env

    // =====================================================================
    //  CI / CD
    // =====================================================================

    // Q23: CI vs CD vs CD?
    // A : Continuous Integration - every commit builds + tests automatically.
    //     Continuous Delivery    - artifacts always deployable; manual gate.
    //     Continuous Deployment  - auto-deploy to prod on green build.

    // Q24: Typical .NET pipeline stages?
    //   1) Restore  (dotnet restore)
    //   2) Build    (dotnet build --no-restore -c Release)
    //   3) Test     (dotnet test --no-build --collect:"XPlat Code Coverage")
    //   4) Pack     (dotnet publish / docker build)
    //   5) Scan     (CodeQL, dependency-check, container scan)
    //   6) Push     (NuGet feed / container registry)
    //   7) Deploy   (per environment with approvals)

    // Q25: GitHub Actions sample (matrix + cache).
    //   /// name: build
    //   /// on: [push, pull_request]
    //   /// jobs:
    //   ///   build:
    //   ///     runs-on: ubuntu-latest
    //   ///     steps:
    //   ///       - uses: actions/checkout@v4
    //   ///       - uses: actions/setup-dotnet@v4
    //   ///         with: { dotnet-version: '8.0.x' }
    //   ///       - run: dotnet restore
    //   ///       - run: dotnet build --no-restore -c Release
    //   ///       - run: dotnet test  --no-build  -c Release

    // Q26: Azure DevOps Pipelines vs GitHub Actions?
    // A : Same idea: YAML pipelines, agents, secrets, environments.
    //     Both integrate with Azure deployments. ADO has Classic UI pipelines.

    // Q27: Where do secrets live in pipelines?
    // A : Pipeline secrets / variable groups / Key Vault-backed groups.
    //     Never echo to logs; mask in YAML, audit access.

    // Q28: Versioning artifacts.
    // A : - SemVer: MAJOR.MINOR.PATCH.
    //     - GitVersion / Nerdbank.GitVersioning for auto bumps.
    //     - Tag images by commit SHA + a moving "latest" only for dev.

    // Q29: Blue/Green and Canary on Azure?
    // A : - App Service: deployment slots + swap.
    //     - AKS: two Deployments with a Service selector swap, or use
    //            Argo Rollouts / Flagger for canary with metrics gating.
    //     - Front Door / AGW weighted routing.

    // Q30: Infrastructure as Code?
    // A : Bicep (Azure-native), Terraform (multi-cloud), Pulumi (real code).
    //     Keep IaC in git, PR-reviewed, applied by pipelines (not by hand).

    // =====================================================================
    //  SCENARIOS
    // =====================================================================

    // [Scenario] Q31: Image works locally but is 800 MB.
    // A : Use multi-stage build, switch to chiseled/aspnet base image,
    //     enable Native AOT (8+), strip pdb, remove dev tools.

    // [Scenario] Q32: Pod restarts every few minutes ("CrashLoopBackOff").
    // A : Check kubectl logs + describe. Common causes:
    //     liveness probe too aggressive, OOMKilled (raise memory limit),
    //     missing config/secret, can't reach DB.

    // [Scenario] Q33: A deployment broke prod; how to roll back?
    // A : kubectl rollout undo deployment/myapp        (K8s)
    //     az webapp deployment slot swap ...           (App Service swap back)
    //     git revert + redeploy                         (last resort)

    // [Scenario] Q34: Two engineers committed to main; CI build fails on
    //   merge. How to fix?
    // A : Pull --rebase, resolve conflicts locally, re-push. Long term:
    //     require PRs with status checks; "merge queue" to serialize merges.

    // [Scenario] Q35: Secrets accidentally committed to git history.
    // A : Rotate the secret IMMEDIATELY (it's compromised). Then rewrite
    //     history with git filter-repo / BFG; force-push; notify team.
    //     Add pre-commit hooks (truffleHog, gitleaks) to prevent recurrence.

    // [Scenario] Q36: Build is slow (10 min). What to optimize?
    // A : - Cache NuGet (~/.nuget/packages) and Docker layers.
    //     - Parallelize tests; split into multiple jobs.
    //     - Use --no-restore / --no-build after the first step.
    //     - Run on bigger agents; pin SDK version.

    // [Scenario] Q37: Need ZERO-downtime DB migration during deploy.
    // A : Expand-Contract migrations + feature flag for the new code path.
    //     Deploy code first (handles both schemas), then schema change,
    //     then cleanup release.

    // [Scenario] Q38: Container running as root — risk?
    // A : Privilege escalation if compromised. Add a non-root USER in
    //     Dockerfile; set runAsNonRoot in pod spec; read-only root FS.

    // [Scenario] Q39: How would you implement progressive delivery?
    // A : Canary via Argo Rollouts / Flagger: gradually shift traffic;
    //     auto-rollback if error rate / latency exceed SLOs.

    // [Scenario] Q40: How to debug a production incident?
    // A : - Acknowledge + create incident channel.
    //     - Check dashboards (errors, p99, saturation).
    //     - Recent deploys / config changes -> roll back if suspect.
    //     - Distributed traces for failing requests.
    //     - Mitigate first, root-cause later, write a blameless postmortem.

    internal static class _DevOps { }
}
