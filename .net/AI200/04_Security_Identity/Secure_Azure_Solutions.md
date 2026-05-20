# Secure Azure Solutions (AZ-204)

> **One-liner**: Security = **Identity + Secrets + Network + Data + Monitoring** — apply defense-in-depth so a single failure doesn't breach the system.

---

## 1. The 5 Pillars (memorize)

| Pillar | Tools |
|---|---|
| **Identity** | Entra ID, Managed Identity, RBAC, Conditional Access |
| **Secrets** | Key Vault (secrets, keys, certs) |
| **Network** | NSG, Private Endpoint, WAF, Firewall, Front Door |
| **Data** | Encryption at rest/in transit, CMK, Always Encrypted |
| **Monitoring** | Defender for Cloud, Sentinel, Activity Log, Insights |

---

## 2. Azure Key Vault — the secret store

| Item | What |
|---|---|
| **Secrets** | Strings (connection strings, passwords) |
| **Keys** | Crypto keys (RSA, EC) for sign/verify, encrypt/decrypt |
| **Certificates** | X.509 certs with auto-renewal |

### Access models (pick ONE per vault)

| Model | When |
|---|---|
| **Azure RBAC** (recommended) | Modern — uses RBAC roles like *Key Vault Secrets User* |
| **Vault access policies** (legacy) | Pre-RBAC — per-principal permissions |

```powershell
# Create
az keyvault create -g rg1 -n kv1 --enable-rbac-authorization true \
  --enable-purge-protection true --enable-soft-delete true

# Grant role
az role assignment create --assignee <miObjectId> \
  --role "Key Vault Secrets User" \
  --scope $(az keyvault show -n kv1 --query id -o tsv)
```

### Read from .NET via MI
```csharp
// dotnet add package Azure.Security.KeyVault.Secrets
// dotnet add package Azure.Identity
var sc = new SecretClient(new Uri("https://kv1.vault.azure.net"), new DefaultAzureCredential());
var db = (await sc.GetSecretAsync("DbConn")).Value.Value;
```

### Key Vault references in App Service / Functions
```
@Microsoft.KeyVault(SecretUri=https://kv1.vault.azure.net/secrets/DbConn/abcd123)
```

> **Pitfall**: Soft delete + purge protection should be **on** in prod (compliance + accidental delete recovery).

---

## 3. Managed Identity (the #1 best practice)

- No secrets in code or config.
- Two flavors:
  - **System-assigned**: 1-to-1 with resource; deleted with it.
  - **User-assigned**: standalone; share across many resources.
- Use `DefaultAzureCredential` from `Azure.Identity` to work both **locally** (dev creds) and **in Azure** (MI).

```csharp
var cred = new DefaultAzureCredential();
// chain: Env → Workload Identity → Managed Identity → VS / VS Code / Azure CLI / Interactive
```

---

## 4. RBAC — least privilege

| Built-in role | Use |
|---|---|
| Reader | View only |
| Contributor | All except access mgmt |
| Owner | Full + grant access |
| User Access Administrator | Manage RBAC only |
| Data-plane roles | e.g. `Storage Blob Data Reader`, `Key Vault Secrets User` |

Custom roles via JSON `roleDefinition`. Scope = subscription / RG / resource.

> **Rule**: Grant the **lowest** data-plane role on the **smallest** scope.

---

## 5. Network Security

### NSG (subnet/NIC firewall)
- Stateful. 5-tuple rules. Default-deny inbound from internet.

### Service Endpoints vs Private Endpoints

| | Service Endpoint | **Private Endpoint** (preferred) |
|---|---|---|
| What | Allows VNet to reach PaaS over Azure backbone | Brings PaaS **into** your VNet via private IP |
| Endpoint | Public IP (locked to your VNet) | Private IP inside your subnet |
| DNS | Public DNS | **Private DNS zone** required |
| Cross-region | Same region | Anywhere |

### WAF / Front Door / App Gateway
- **WAF** = OWASP Top 10 protection (SQLi, XSS).
- **Application Gateway WAF** = regional L7 LB.
- **Front Door WAF** = global L7, anycast.
- **DDoS Protection Standard** = subscription-wide.

### Azure Firewall
- Stateful network firewall for VNet egress, app/network/NAT rules.
- Forced tunneling, FQDN filtering, threat intel.

---

## 6. Data Encryption

| Layer | Default | Customer-managed |
|---|---|---|
| **At rest** (Storage/SQL/Cosmos/Disks) | Microsoft-managed keys (AES-256) | **CMK** in Key Vault (BYOK) |
| **In transit** | TLS 1.2+ | Mutual TLS optional |
| **In use** | — | **Confidential VMs / SQL Always Encrypted with enclaves** |

> Always enforce **TLS 1.2** minimum (`Min TLS = 1.2`) on Storage, SQL, App Service.

---

## 7. Microsoft Defender for Cloud

- Free CSPM (Cloud Security Posture Management) — Secure Score + recommendations.
- Paid plans (per resource): Defender for Storage, SQL, App Service, Key Vault, Containers, etc.
- Outputs: alerts, regulatory compliance dashboards (PCI, ISO, CIS).

---

## 8. Azure Sentinel (SIEM/SOAR)

- Built on Log Analytics.
- Ingests logs from anything (Azure, M365, AWS, on-prem).
- KQL queries → analytic rules → incidents.
- Playbooks (Logic Apps) automate response.

---

## 9. Logging & Audit

| Source | Where it goes |
|---|---|
| **Activity Log** | Subscription-level control plane actions |
| **Resource logs (diagnostic settings)** | Per-resource — route to Log Analytics, Storage, Event Hub |
| **App Insights** | App telemetry |
| **Entra sign-in/audit logs** | Identity events |

Centralize in **Log Analytics workspace**, retain per compliance policy.

---

## 10. OWASP Top 10 in Azure context

| OWASP | Azure-side mitigation |
|---|---|
| Broken access control | Use **Authorization policies**, RBAC, app roles |
| Cryptographic failures | Enforce TLS, CMK with Key Vault, no custom crypto |
| Injection | Parameterize queries, validate input, EF Core / TVPs |
| Insecure design | Threat model early; Front Door + WAF |
| Security misconfig | Defender Secure Score, IaC scanning |
| Vulnerable components | Dependabot, `dotnet list package --vulnerable` |
| Auth failures | Use Entra + MFA + Conditional Access |
| Software/data integrity | Sign images, SBOM, ACR Content Trust / Notation |
| Logging failures | App Insights + Diagnostic Settings → LA |
| SSRF | Validate URLs, block instance metadata IP (`169.254.169.254`) at egress |

---

## 11. Application-Level Hardening (ASP.NET Core)

```csharp
app.UseHttpsRedirection();
app.UseHsts();

// Security headers (Microsoft.AspNetCore.SecurityHeaders)
app.UseSecurityHeaders(p => p
    .AddDefaultSecurityHeaders()
    .AddContentSecurityPolicy(b => b.AddDefaultSrc().Self()));

// Rate limiting (.NET 7+)
builder.Services.AddRateLimiter(o =>
    o.AddFixedWindowLimiter("api", l => { l.PermitLimit = 100; l.Window = TimeSpan.FromMinutes(1); }));
app.UseRateLimiter();

// CORS (allowlist)
builder.Services.AddCors(o => o.AddPolicy("p", b => b
    .WithOrigins("https://app.contoso.com").AllowAnyHeader().AllowAnyMethod()));
```

---

## 12. Secrets in Code — anti-patterns to spot

| Bad | Good |
|---|---|
| `"Server=...;Pwd=Pa$$w0rd"` in `appsettings.json` | Key Vault reference + MI |
| Hard-coded SAS in URL | User Delegation SAS, short expiry |
| Connection string in env var pushed to repo | Key Vault + `DefaultAzureCredential` |
| Client secret with no rotation | **Certificate** or MI |
| Logging tokens / PII | Sanitize logs, use redaction |

---

## 13. CI/CD Security

- **OIDC federation** for GitHub Actions / DevOps → no secrets in pipelines.
- Scan images (Defender for Containers, Trivy).
- Scan IaC (Checkov, PSRule for Azure).
- **Code signing** for binaries, **image signing** for containers.
- Branch protection + required reviews for `main`.

---

## 14. Compliance Quick Hits

| Cert | Notes |
|---|---|
| ISO 27001, SOC 2 | General enterprise |
| PCI-DSS | Payments |
| HIPAA / HITRUST | Healthcare (BAA available) |
| GDPR | EU data subjects; honor data subject requests |
| FedRAMP / IL5 | US Gov (Azure Gov) |

Use **Azure Policy** to enforce ("deny resources without tag X", "deny storage with public access").

---

## 15. Common Pitfalls

| Pitfall | Fix |
|---|---|
| Storage account allows public blob access | Set `AllowBlobPublicAccess=false` |
| KV access policy + RBAC mixed | Pick **one** model per vault |
| MI works locally but not in Azure | `DefaultAzureCredential` chain order — set `AZURE_CLIENT_ID` for user-assigned MI |
| 403 from KV after assigning role | Wait ~30s for replication; use `--scope` of vault not RG |
| Forgot purge protection | Cannot recover deleted vault — enable up front |
| Open NSG `Any/Any` rule | Tighten to specific IPs/ASGs |
| Public IP on a DB | Add Private Endpoint, deny public traffic |

---

## 16. AZ-204 Q&A

**Q1. How do you avoid storing secrets in code?**
**Managed Identity** + **Key Vault** (with Key Vault references in App Service settings).

**Q2. Difference between system-assigned and user-assigned MI?**
System = tied to a single resource's lifecycle. User = standalone, can be shared.

**Q3. RBAC vs Key Vault access policy?**
RBAC is the new model (recommended). Access policies are legacy per-principal permissions. One vault uses one model.

**Q4. Service Endpoint vs Private Endpoint?**
Service Endpoint locks a PaaS service's public endpoint to your VNet (still public IP). Private Endpoint gives the PaaS a private IP **inside** your VNet.

**Q5. How to encrypt with your own key (BYOK)?**
Generate key in Key Vault → enable **CMK** on Storage/SQL/Disk → grant resource MI access to the key.

**Q6. How to enforce MFA only from outside corp network?**
Conditional Access policy with location condition + Grant: Require MFA.

**Q7. What is `DefaultAzureCredential`?**
A credential chain trying env vars → workload identity → MI → CLI → VS → interactive — same code works locally and in Azure.

**Q8. Soft delete vs purge protection in KV?**
Soft delete retains deleted items N days; purge protection blocks even admins from permanent delete during retention.

**Q9. How to get logs centrally from many resources?**
Diagnostic settings → **Log Analytics workspace**; query with KQL.

**Q10. Defender for Cloud vs Sentinel?**
Defender = CSPM/CWPP (Azure-resource focused). Sentinel = SIEM/SOAR (correlates logs across estate; incidents & playbooks).

**Q11. Where to put Key Vault references?**
App Service / Function App settings with the `@Microsoft.KeyVault(...)` syntax — resolved at startup.

**Q12. WAF L7 protection?**
Application Gateway WAF (regional) or Front Door WAF (global) with OWASP Core Rule Set.

---

## 17. Mental Model

> **No secrets in code (MI + Key Vault). Lock down the network (Private Endpoints + WAF). Enforce identity (Entra + RBAC + CA + MFA). Encrypt everything (TLS 1.2 + CMK). Watch everything (Defender + Sentinel + Diagnostic Settings).**
