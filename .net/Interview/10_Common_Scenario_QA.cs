// =====================================================================
//  10) COMMON SCENARIO Q&A — Real-world tricky questions
// =====================================================================
namespace Interview.Scenarios
{
    // ---------------------------------------------------------------------
    // [Scenario] Q1: Design a paginated, filterable, sortable products API.
    // A : GET /api/products?search=foo&category=2&sort=-price&page=1&size=20
    //     - Validate page/size bounds.
    //     - Return { items, totalCount, page, size } or use Link headers.
    //     - Project to DTOs; index DB columns used in filters.
    //     - Use AsNoTracking().
    // ---------------------------------------------------------------------

    // [Scenario] Q2: How would you implement file upload + virus scan?
    // A : Multipart streaming upload to blob storage -> publish event ->
    //     scanner worker -> mark file Clean/Infected -> notify client.
    //     Don't keep the request open while scanning.

    // [Scenario] Q3: Email-sending service should not block the request.
    // A : Enqueue to a background queue (IHostedService, Azure Service Bus,
    //     RabbitMQ). Use the Outbox pattern to avoid losing events if the
    //     transaction fails.

    // [Scenario] Q4: API needs to call 5 microservices in parallel.
    // A : Use Task.WhenAll with IHttpClientFactory; add Polly resilience
    //     (timeout, retry, circuit breaker). Aggregate results, decide on
    //     partial-failure response shape.

    // [Scenario] Q5: How to handle multi-tenant data isolation?
    // A : Option A: tenant column + global query filter.
    //     Option B: schema per tenant.
    //     Option C: database per tenant.
    //     Always pick connection / filter from a TenantContext built from JWT.

    // [Scenario] Q6: Health checks?
    // A : builder.Services.AddHealthChecks().AddDbContextCheck<AppDb>()
    //                                      .AddRedis(redisConn)
    //                                      .AddUrlGroup(new Uri("https://api/..."));
    //     app.MapHealthChecks("/health"); separate "/ready" and "/live" probes.

    // [Scenario] Q7: How to localize an MVC app?
    // A : Resx files + IStringLocalizer<T>. Configure RequestLocalization with
    //     supported cultures and providers (Cookie, AcceptLanguage, Query).

    // [Scenario] Q8: A controller endpoint must be reachable only by an
    //   internal service, not by users.
    // A : - Restrict by IP (middleware).
    //     - Or use a separate auth scheme / "internal" policy.
    //     - Or mount under a different port/host bound to private network.

    // [Scenario] Q9: How would you implement soft delete?
    // A : Bool IsDeleted on entities + global query filter to exclude them.
    //     Override SaveChanges to set IsDeleted=true instead of removing.

    // [Scenario] Q10: How to test a controller that depends on DbContext?
    // A : - Unit test: mock repository / use UseInMemoryDatabase.
    //     - Integration test: WebApplicationFactory<TProgram> + SQLite/
    //       Testcontainers; assert HTTP responses end-to-end.

    // [Scenario] Q11: Long-running export of millions of rows for a user.
    // A : Don't stream synchronously. Job pattern:
    //     1) POST /exports -> 202 + Location: /exports/{id}
    //     2) Worker generates CSV to blob storage.
    //     3) GET /exports/{id} returns 200 + SAS URL when done.

    // [Scenario] Q12: Race condition: two requests increment a counter,
    //   final value is wrong.
    // A : - DB-level: UPDATE ... SET cnt = cnt + 1 (atomic).
    //     - App-level: lock per key, distributed lock (Redis), or
    //       optimistic concurrency with retries.

    // [Scenario] Q13: How would you migrate a monolith to microservices?
    // A : Strangler-fig: extract bounded contexts gradually; use a gateway
    //     to route old/new; share schema only via APIs; introduce async
    //     events for decoupling (outbox + bus); add observability first.

    // [Scenario] Q14: Logs in production are noisy; how to control verbosity?
    // A : appsettings.{Env}.json with LogLevel categories;
    //     use Serilog with sinks; structured logging (key=value); sample
    //     debug logs; correlate with TraceId/SpanId.

    // [Scenario] Q15: Same code, faster on dev than prod. Possible causes?
    // A : - Cold start / no JIT tiering warmed up.
    //     - Different GC mode (Workstation vs Server).
    //     - Network latency to DB/cache.
    //     - Resource limits (containers).
    //     - Different appsettings (logging level Debug, etc.).

    // [Scenario] Q16: How would you implement feature flags?
    // A : Microsoft.FeatureManagement; flag values from config / Azure App
    //     Configuration; gate code with IFeatureManager / [FeatureGate].

    // [Scenario] Q17: How to call legacy SOAP from a modern API?
    // A : Add Service Reference (dotnet-svcutil) generates client; wrap in
    //     an interface; mock for tests; add resilience (Polly).

    // [Scenario] Q18: You inherit a project with no tests. Where to start?
    // A : Characterization tests (capture current behavior), focus on the
    //     riskiest module first, add integration tests at API boundary,
    //     then refactor under the safety net.

    // [Scenario] Q19: How do you deploy zero-downtime?
    // A : Blue/green or rolling deploys; DB migrations must be backward
    //     compatible (expand-contract); health checks gate traffic; warm-up
    //     hits before adding to LB.

    // [Scenario] Q20: A user reports a bug only THEY hit. Process?
    // A : Reproduce with their input; check logs by correlation id;
    //     inspect data (tenant-specific rows, feature flag, locale).
    //     Add a regression test before fixing.

    internal static class _Scenarios { }
}
