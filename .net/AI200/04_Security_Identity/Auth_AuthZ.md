# Authentication & Authorization on Azure (AZ-204)

> **One-liner**: **AuthN = who you are** (sign-in), **AuthZ = what you can do** (permissions). On Azure, the engine for both is **Microsoft Entra ID** + **OAuth 2.0 / OpenID Connect**.

---

## 1. Vocabulary

| Term | Meaning |
|---|---|
| **Microsoft Entra ID** | New name for Azure AD — cloud identity service |
| **Tenant** | An instance of Entra (your org's directory) |
| **App registration** | Identity for a custom app (client + API) |
| **Enterprise application** | The "service principal" object in your tenant |
| **Service principal (SP)** | Runtime identity of an app in a tenant |
| **Managed Identity (MI)** | SP automatically managed by Azure (no secrets) |
| **Scope** | A permission name on an API (e.g., `Files.Read`) |
| **Role** | Permission grouping (RBAC for Azure; app role for app) |
| **Claim** | Statement inside a token (`sub`, `roles`, `scp`) |

---

## 2. OAuth 2.0 vs OpenID Connect

| | OAuth 2.0 | OpenID Connect (OIDC) |
|---|---|---|
| Purpose | **Authorization** (access an API) | **Authentication** (sign-in) |
| Returns | **Access token** | **ID token** (+ access token) |
| Built on | — | OAuth 2.0 |

> Rule: **OIDC for sign-in, OAuth for calling APIs**. Most modern web apps use both together.

---

## 3. OAuth Flows (pick the right one)

| Flow | Who uses it |
|---|---|
| **Authorization Code + PKCE** | SPAs, native/mobile, modern web — **default** |
| **Authorization Code (confidential)** | Server-side web app with client secret |
| **Client Credentials** | Daemon/service-to-service (no user) |
| **On-Behalf-Of (OBO)** | API calls another API as the original user |
| **Device Code** | TVs, CLIs, no-browser devices |
| ~~Implicit~~ | Deprecated |
| ~~ROPC~~ | Don't use (only legacy / no-browser) |

**Memory hint**: User present → **Auth Code + PKCE**. No user → **Client Credentials**.

---

## 4. Tokens

| Token | Audience | Format | Lifetime |
|---|---|---|---|
| **ID token** | The client | JWT | ~1 hr |
| **Access token** | Target API | JWT | ~1 hr |
| **Refresh token** | The IdP | Opaque | ~24-90 days, single-use |

### JWT anatomy
```
header.payload.signature
```
Always **validate**: issuer (`iss`), audience (`aud`), expiry (`exp`/`nbf`), signature (with JWKS keys).

---

## 5. ASP.NET Core — sign in users with Entra (OIDC)

```csharp
// dotnet add package Microsoft.Identity.Web
// dotnet add package Microsoft.Identity.Web.UI

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
```

`appsettings.json`:
```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId": "<tenant-guid>",
  "ClientId": "<app-client-id>",
  "CallbackPath": "/signin-oidc"
}
```

---

## 6. ASP.NET Core — protect a Web API with Entra

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(opts =>
{
    // Require scope (delegated)
    opts.AddPolicy("Read", p => p.RequireClaim("scp", "Files.Read"));
    // Require app role (app-only)
    opts.AddPolicy("Admin", p => p.RequireRole("Admin"));
});

app.MapGet("/files", () => "ok").RequireAuthorization("Read");
```

---

## 7. Call a downstream API as the user (OBO)

```csharp
builder.Services
    .AddMicrosoftIdentityWebApiAuthentication(builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddDownstreamApi("Graph", builder.Configuration.GetSection("Graph"))
    .AddInMemoryTokenCaches();

public async Task<IActionResult> Me([FromServices] IDownstreamApi api)
    => Ok(await api.GetForUserAsync<dynamic>("Graph", o => o.RelativePath = "me"));
```

---

## 8. Service-to-service (no user) — Client Credentials

```csharp
var app = ConfidentialClientApplicationBuilder
    .Create(clientId)
    .WithClientSecret(secret)        // or .WithCertificate(cert) — preferred
    .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
    .Build();

var result = await app.AcquireTokenForClient(new[] { "api://api-id/.default" }).ExecuteAsync();
```

> Better: use **Managed Identity** so there's no secret at all.

---

## 9. Managed Identity (MI) — the killer feature

- Azure-managed identity with **no secret to rotate**.
- Two kinds:
  - **System-assigned**: tied to resource lifecycle.
  - **User-assigned**: standalone, attach to many resources.

```csharp
// Works locally (dev creds) AND in Azure (MI)
var cred = new DefaultAzureCredential();
var blob = new BlobServiceClient(new Uri("https://acct.blob.core.windows.net"), cred);
```

Grant the role on the resource:
```powershell
az role assignment create --assignee <miPrincipalId> \
  --role "Storage Blob Data Contributor" \
  --scope /subscriptions/<sub>/resourceGroups/rg1/providers/Microsoft.Storage/storageAccounts/acct
```

---

## 10. Azure RBAC vs App Roles vs Groups

| Mechanism | Where | What it controls |
|---|---|---|
| **Azure RBAC** | Azure control plane (resources) | Who can manage what (Owner, Contributor, Reader, custom) |
| **App roles** | Inside an app's manifest | What features users see (Admin, Editor) |
| **Entra Groups** | Directory groups | Assign roles in bulk |
| **Conditional Access** | Tenant-wide policies | When/how sign-in is allowed (MFA, location) |

---

## 11. App Roles & Scopes — how authorization shows in token

```json
// Delegated (user is signed in)
"scp": "Files.Read Files.Write"

// App-only (daemon)
"roles": ["Admin", "Reader"]
```

`[Authorize(Policy = "...")]` matches these claims.

---

## 12. Microsoft Graph

- Central API for Entra users, groups, mail, files, Teams.
- Use **Graph SDK** + Entra token with proper scope (e.g., `User.Read`).
- Permissions are either **delegated** (user) or **application** (daemon).

---

## 13. App Service "Easy Auth"

- Built-in middleware in front of your app.
- Configure providers: Entra ID, Google, FB, X, Apple, OIDC.
- Three modes: Allow anonymous, Require auth, Custom.
- User info forwarded as headers: `X-MS-CLIENT-PRINCIPAL`, `X-MS-CLIENT-PRINCIPAL-ID`.

---

## 14. B2B vs B2C

| | B2B (External ID) | B2C (External ID for customers) |
|---|---|---|
| Users | Guests from other Entra tenants / partners | Custom-branded sign-up/sign-in for consumers |
| Identity providers | Entra, federated | Local accounts, social (Google/FB/Apple), SAML |
| Tenant | Your workforce tenant | Separate external tenant |
| Use case | Vendor portal | Public-facing app |

---

## 15. Security Best Practices

| Topic | Do |
|---|---|
| Secrets | Use **Managed Identity** > certificates > secrets |
| Tokens | Validate `iss`, `aud`, `exp`, `nbf`, signature |
| HTTPS | Required everywhere (`UseHttpsRedirection`) |
| CORS | Allow only known origins |
| State / Nonce | Use library defaults — prevents CSRF/replay |
| Refresh tokens | Keep server-side (confidential clients) |
| Logout | Call `/signout-oidc` + clear cookie |
| Reply URLs | Pin exact URLs in app registration |
| Cert rotation | Use Key Vault auto-rotation |

---

## 16. Common Pitfalls

| Pitfall | Fix |
|---|---|
| `IDX10501` audience mismatch | API expects `api://<id>` — set `Audience` correctly |
| Implicit/ROPC used | Switch to **Auth Code + PKCE** |
| Secrets in source | Use MI / Key Vault |
| Role missing in token | Admin consent not granted, or role not assigned to user |
| 401 instead of 403 | Token invalid (sig/exp) — fix token, not policy |
| Long-lived sessions | Use refresh token + idle timeout policy |
| Token forwarded to wrong API | Each downstream call needs its **own** access token |

---

## 17. AZ-204 Q&A

**Q1. OAuth vs OIDC?**
OAuth = authorize API access (access token). OIDC = authenticate user (ID token), built on OAuth.

**Q2. Which OAuth flow for an SPA?**
**Authorization Code + PKCE** (no client secret).

**Q3. Which flow for a daemon hitting Graph?**
**Client Credentials** with application permissions (or Managed Identity).

**Q4. Difference between delegated and application permission?**
Delegated = "act on behalf of signed-in user" (scopes / `scp`). Application = "act as itself" (app roles / `roles`).

**Q5. How does an API validate a JWT?**
Verify signature against IdP's JWKS, check `iss`, `aud`, `exp`, `nbf`, optionally `tid` / claims.

**Q6. What is PKCE and why?**
Proof Key for Code Exchange — binds the auth code to the original requester so a stolen code can't be redeemed.

**Q7. On-Behalf-Of flow?**
API receives user token, exchanges it at the IdP for a new token to call **another** API as the same user.

**Q8. Managed Identity vs Service Principal?**
MI = SP whose credentials Azure manages automatically. Same trust model, no secret you handle.

**Q9. How does App Service Easy Auth work?**
A platform module intercepts requests, validates tokens, injects user claims via headers — no code in your app.

**Q10. Difference between B2B and B2C?**
B2B = invite external workforce users into your tenant. B2C = separate external tenant with custom-branded consumer sign-up.

**Q11. Where do you store an Entra ID client secret?**
Don't — prefer Managed Identity. Otherwise **Key Vault** + Key Vault reference.

**Q12. How to require MFA only when accessing finance app from outside corp network?**
**Conditional Access** policy: target app + location condition + grant MFA.

---

## 18. CLI Cheat Sheet

```powershell
# Create app registration
az ad app create --display-name myapi --sign-in-audience AzureADMyOrg
az ad sp create --id <appId>

# Add MI to App Service
az webapp identity assign -g rg1 -n myapp

# Grant role
az role assignment create --assignee <objectId> --role "Storage Blob Data Reader" \
  --scope /subscriptions/<sub>/resourceGroups/rg1/providers/Microsoft.Storage/storageAccounts/acct

# Add API scope / app role — via portal or Microsoft Graph

# Test token
$tok = (az account get-access-token --resource "https://graph.microsoft.com").accessToken
Invoke-RestMethod -Uri "https://graph.microsoft.com/v1.0/me" -Headers @{ Authorization = "Bearer $tok" }
```

---

## 19. Mental Model

> **AuthN = OIDC (ID token). AuthZ = OAuth (access token + scopes/roles). Use Managed Identity to remove secrets, RBAC to scope access, and Conditional Access to control conditions.**
