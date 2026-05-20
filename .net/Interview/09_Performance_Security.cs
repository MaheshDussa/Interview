// =====================================================================
//  09) PERFORMANCE & SECURITY — Q&A and Scenarios
// =====================================================================
namespace Interview.PerfSec
{
    // ---------------------------------------------------------------------
    //  PERFORMANCE
    // ---------------------------------------------------------------------

    // Q1: How to diagnose a slow ASP.NET Core app?
    // A : Application Insights / OpenTelemetry, dotnet-trace, dotnet-counters,
    //     PerfView, MiniProfiler, BenchmarkDotNet for micro-benchmarks.

    // Q2: Response caching vs Output caching vs IMemoryCache vs IDistributedCache?
    // A : Response caching - HTTP-cache headers, client-side.
    //     Output caching   - server-side cache of response (NEW in .NET 7).
    //     IMemoryCache     - per-instance, fast, lost on restart.
    //     IDistributedCache- Redis/SQL; survives restart; cross-instance.
    //     HybridCache (.NET 9) - L1 (memory) + L2 (distributed) combined.

    // Q3: How to reduce allocations?
    // A : Span<T>/Memory<T>, ArrayPool<T>, stackalloc, struct over class,
    //     StringBuilder, avoid LINQ in hot paths, ValueTask for hot async.

    // Q4: ValueTask vs Task?
    // A : ValueTask avoids allocating a Task when the result is already
    //     available (cached/synchronous). Use for hot, frequently-sync paths.

    // Q5: When does the GC become a problem?
    // A : High-frequency allocations on the LOH, gen-2 collections, finalizer
    //     queues. Server GC vs Workstation GC. Watch with dotnet-counters.

    // ---------------------------------------------------------------------
    //  SECURITY (think OWASP Top 10)
    // ---------------------------------------------------------------------

    // Q6: SQL Injection — how to prevent?
    // A : Parameterized queries / ORMs. Never concatenate user input into SQL.
    //     EF interpolated SQL parameterizes automatically.

    // Q7: XSS (Cross-Site Scripting) — prevention?
    // A : Razor auto-encodes by default (@model.Name). Use Html.Raw ONLY for
    //     trusted content. Sanitize user-supplied HTML if needed.

    // Q8: CSRF — prevention?
    // A : Anti-forgery token in forms ([ValidateAntiForgeryToken]) — auto for
    //     Razor. For APIs using cookies, validate origin + SameSite cookies.

    // Q9: Storing passwords?
    // A : Never plain. Use PBKDF2/BCrypt/Argon2 (ASP.NET Identity does this).
    //     Salt is per-user, included in hash.

    // Q10: JWT — best practices?
    // A : - Sign with strong key (HS256 or RS256).
    //     - Short lifetime + refresh tokens.
    //     - Validate Issuer, Audience, Lifetime, Signature.
    //     - Don't put secrets in the token (it's base64, not encrypted).
    //     - Rotate signing keys; use Key ID (kid).

    // Q11: OAuth2 vs OpenID Connect?
    // A : OAuth2 = authorization (access to resources).
    //     OIDC   = identity layer ON TOP of OAuth2 (who the user is).

    // Q12: HTTPS in ASP.NET Core?
    // A : app.UseHttpsRedirection(); app.UseHsts(); HTTPS cert via ACME
    //     (Let's Encrypt) / managed cert. Behind LB, use forwarded headers.

    // Q13: Secrets management?
    // A : Never check secrets into git. Use:
    //     - User Secrets (dev): dotnet user-secrets set ...
    //     - Azure Key Vault / AWS Secrets Manager (prod)
    //     - Environment variables for container runtimes

    // Q14: Data Protection API?
    // A : Built-in encryption/signing of cookies, anti-forgery, etc.
    //     In a farm, configure shared key storage (e.g., Azure Blob, Redis).

    // Q15: Headers to harden?
    // A : Strict-Transport-Security, X-Content-Type-Options: nosniff,
    //     Referrer-Policy, Content-Security-Policy, X-Frame-Options.

    // ---------------------------------------------------------------------
    //  SCENARIOS
    // ---------------------------------------------------------------------
    // [Scenario] Q16: API CPU spikes to 100%. Steps?
    // A : 1) dotnet-counters for live metrics.
    //     2) dotnet-dump for snapshot; analyze with VS or dotnet-dump analyze.
    //     3) Look for tight loops, JSON serialization on huge payloads,
    //        regex catastrophic backtracking, sync-over-async.

    // [Scenario] Q17: Memory grows until OutOfMemory. Approach?
    // A : Check for static event handlers (leaks), unbounded caches,
    //     HttpClient instances not reused (use IHttpClientFactory),
    //     captured DbContexts.

    // [Scenario] Q18: A penetration test flagged JWTs visible to JS.
    // A : Store JWT in HttpOnly + Secure cookie OR use BFF pattern with
    //     short-lived access tokens + refresh on server.

    // [Scenario] Q19: Multiple instances behind a load balancer lose
    //   anti-forgery / Identity cookies on failover.
    // A : Data Protection keys aren't shared. Configure shared key ring.

    // [Scenario] Q20: A search endpoint allows arbitrary regex from users.
    //   What's the risk?
    // A : ReDoS — a pathological regex causes exponential backtracking.
    //     Use timeout (Regex options) or whitelist patterns.

    internal static class _PerfSec { }
}
