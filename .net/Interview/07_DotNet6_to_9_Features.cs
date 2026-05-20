// =====================================================================
//  07) .NET 6 -> .NET 9 — What's New (Interview-Relevant)
// =====================================================================
namespace Interview.WhatsNew
{
    // ---------------------------------------------------------------------
    //  .NET 6 (LTS, Nov 2021)
    // ---------------------------------------------------------------------
    // • Minimal APIs:
    //   /// var builder = WebApplication.CreateBuilder(args);
    //   /// var app = builder.Build();
    //   /// app.MapGet("/hello", () => "Hi");
    //   /// app.Run();
    // • Top-level statements (no Main/Program class).
    // • Implicit usings & file-scoped namespaces.
    // • record struct, global usings.
    // • DateOnly, TimeOnly.
    // • Hot Reload.

    // ---------------------------------------------------------------------
    //  .NET 7 (STS, Nov 2022)
    // ---------------------------------------------------------------------
    // • Rate limiting middleware:
    //   /// builder.Services.AddRateLimiter(...);
    //   /// app.UseRateLimiter();
    // • Output caching middleware:
    //   /// builder.Services.AddOutputCache();
    //   /// app.UseOutputCache();
    //   /// app.MapGet("/x", ...).CacheOutput();
    // • Generic math (static abstract members in interfaces).
    // • Required members (`required` keyword).
    // • Raw string literals """ ... """.
    // • EF Core 7: ExecuteUpdate / ExecuteDelete (bulk, no tracking).
    // • Minimal API filters / endpoint groups.

    // ---------------------------------------------------------------------
    //  .NET 8 (LTS, Nov 2023)
    // ---------------------------------------------------------------------
    // • Primary constructors for classes:
    //   /// public class OrderService(IRepo repo, ILogger<OrderService> log)
    //   /// {
    //   ///     public Task RunAsync() => repo.LoadAsync();
    //   /// }
    // • Collection expressions: int[] a = [1, 2, 3];
    //                            List<int> b = [..a, 4];
    // • Keyed services in DI:
    //   /// services.AddKeyedSingleton<IPay, Stripe>("stripe");
    //   /// services.AddKeyedSingleton<IPay, Paypal>("paypal");
    //   /// ctor: ([FromKeyedServices("stripe")] IPay pay)
    // • IExceptionHandler:
    //   /// public class MyHandler : IExceptionHandler {
    //   ///     public ValueTask<bool> TryHandleAsync(...) { ... }
    //   /// }
    //   /// builder.Services.AddExceptionHandler<MyHandler>();
    //   /// app.UseExceptionHandler();
    // • Native AOT for ASP.NET Core (small, fast startup).
    // • Identity API endpoints (MapIdentityApi<TUser>()).
    // • [TimeProvider] (testable time).
    // • HybridCache (preview in 8, stable later).

    // ---------------------------------------------------------------------
    //  .NET 9 (STS, Nov 2024)
    // ---------------------------------------------------------------------
    // • Built-in OpenAPI (Microsoft.AspNetCore.OpenApi):
    //   /// builder.Services.AddOpenApi();
    //   /// app.MapOpenApi();
    // • HybridCache (L1 + L2 distributed) stable:
    //   /// builder.Services.AddHybridCache();
    //   /// await cache.GetOrCreateAsync("key", async ct => await LoadAsync(ct));
    // • Static AOT improvements; smaller container images.
    // • New LINQ methods: CountBy, AggregateBy, Index().
    // • params Span<T> / ReadOnlySpan<T>.
    // • Lock object (System.Threading.Lock) — faster than `lock(obj)`.
    // • Improved Kestrel HTTP/3, better diagnostics.

    // ---------------------------------------------------------------------
    // Q : Why upgrade?
    // A : Even versions (6, 8) are LTS (3-year support).
    //     Odd versions (7, 9) get 18 months — preview newest features.
    //     Pick LTS for production unless you need new features.
    // ---------------------------------------------------------------------

    // [Scenario] You migrate a .NET 6 Web API to .NET 8. Things to verify:
    //   - Update target framework, SDK, base Docker images.
    //   - Check obsolete APIs, nullable warnings.
    //   - Run integration tests; verify Swagger still works.
    //   - Consider switching to primary constructors / keyed services where helpful.
    //   - Reassess startup config: WebApplicationBuilder is now standard.

    // [Scenario] Asked to choose between .NET 8 vs 9 for a new product:
    //   - Long-term enterprise -> .NET 8 (LTS).
    //   - Greenfield, can upgrade yearly -> .NET 9 -> jump to .NET 10 LTS later.

    internal static class _News { }
}
