// =====================================================================
//  03) .NET CORE / ASP.NET CORE — Core Concepts
// =====================================================================
using System;

namespace Interview.DotNetCore
{
    // ---------------------------------------------------------------------
    // Q1: What is .NET / .NET Core / .NET 5+?
    // A : .NET Framework  - Windows-only legacy (last 4.8).
    //     .NET Core 1-3.1 - cross-platform, open-source.
    //     .NET 5/6/7/8/9  - unified successor. Even = LTS (6, 8). Odd = STS.
    // ---------------------------------------------------------------------

    // Q2: CLR vs CoreCLR vs Mono?
    // A : Execution engines for .NET. CoreCLR is used by modern .NET.

    // Q3: What is the difference between IL and JIT?
    // A : C# -> compile -> IL (intermediate language).
    //     CLR's JIT compiles IL to native code at runtime.
    //     AOT (ReadyToRun, NativeAOT) compiles ahead of time for fast startup.

    // ---------------------------------------------------------------------
    // Q4: Explain Dependency Injection (DI) in ASP.NET Core.
    // A : Built-in IoC container. You register services in Program.cs and
    //     the framework injects them via constructor/parameter.
    //
    //   /// builder.Services.AddSingleton<IClock, Clock>();
    //   /// builder.Services.AddScoped<IOrderService, OrderService>();
    //   /// builder.Services.AddTransient<IEmailSender, EmailSender>();
    // ---------------------------------------------------------------------

    // Q5: Service lifetimes — when to use which?
    // A : Singleton - one per app (e.g., config, cache). Must be thread-safe.
    //     Scoped    - one per HTTP request (e.g., DbContext). Default for EF.
    //     Transient - new every injection (lightweight stateless services).
    //
    // [Scenario] Captive dependency: Singleton depends on Scoped -> Scoped
    //  is captured forever and behaves like Singleton (bug).

    // ---------------------------------------------------------------------
    // Q6: What is the middleware pipeline?
    // A : Ordered chain of components handling HTTP requests/responses.
    //     Each can short-circuit or call the next.
    //
    //   /// app.UseExceptionHandler("/error");
    //   /// app.UseHttpsRedirection();
    //   /// app.UseRouting();
    //   /// app.UseAuthentication();   // BEFORE Authorization
    //   /// app.UseAuthorization();
    //   /// app.MapControllers();
    //
    //  Order matters! e.g., Authorization before Routing won't know the endpoint.
    // ---------------------------------------------------------------------

    // Q7: Difference between app.Use, app.Run, app.Map?
    // A : Use - adds middleware; calls next.
    //     Run - terminal middleware (doesn't call next).
    //     Map - branches the pipeline based on path.

    // Q8: How does ASP.NET Core host the app?
    // A : Generic Host (IHostBuilder) -> WebHost -> Kestrel server.
    //     Kestrel can run alone or behind IIS/Nginx as reverse proxy.

    // Q9: appsettings.json + Environment-specific config?
    // A : Layered config: appsettings.json -> appsettings.{Env}.json ->
    //     user secrets -> env vars -> command-line. Later wins.

    // Q10: Options pattern?
    // A : Bind config sections to strongly-typed classes:
    //
    //   /// builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
    //   /// public class UseIt(IOptions<JwtOptions> opt) { ... }
    //
    //   IOptions          - singleton, no reload
    //   IOptionsSnapshot  - scoped, reloads each request
    //   IOptionsMonitor   - singleton with change notifications

    // ---------------------------------------------------------------------
    // Q11: ILogger<T> — why generic?
    // A : Category = full type name; lets filtering by namespace/class:
    //     "Logging": { "LogLevel": { "MyApp.Orders": "Debug" } }
    // ---------------------------------------------------------------------

    // Q12: IHostedService / BackgroundService?
    // A : Long-running background tasks tied to host lifetime
    //     (timers, queue consumers, schedulers).

    // Q13: Hosted environment IHostEnvironment vs IWebHostEnvironment?
    // A : IHostEnvironment is the modern one. Has ContentRootPath +
    //     EnvironmentName. IWebHostEnvironment also has WebRootPath (wwwroot).

    // ---------------------------------------------------------------------
    // [Scenario] Q14: Your singleton service takes IHttpContextAccessor and
    //   sometimes returns wrong user. Why?
    // A : HttpContext is per-request. Capturing it in a singleton creates a
    //     captive dependency / race. Always resolve through the accessor at
    //     the call site or use Scoped.
    // ---------------------------------------------------------------------

    // [Scenario] Q15: An action returning Task<IActionResult> hangs only on
    //   production. Why?
    // A : Likely sync-over-async (.Result/.Wait) somewhere — under load it
    //     starves the thread pool. Fix: full async chain.

    // [Scenario] Q16: How would you add a global exception handler?
    // A : app.UseExceptionHandler("/error") + an [ApiController] returning
    //     ProblemDetails, OR an IExceptionHandler (8+), OR custom middleware.

    // [Scenario] Q17: Service throws "Cannot consume scoped service from
    //   singleton". How to fix without changing lifetime?
    // A : Inject IServiceScopeFactory; create a scope when needed:
    //     using var scope = _factory.CreateScope();
    //     var svc = scope.ServiceProvider.GetRequiredService<IScopedSvc>();

    // [Scenario] Q18: You need to swap implementation of an interface per
    //   tenant. Approach?
    // A : Register a factory: services.AddScoped<IRepo>(sp => {
    //         var tenant = sp.GetRequiredService<ITenant>();
    //         return tenant.Id == "A" ? new RepoA() : new RepoB(); });

    internal static class _Concepts { }
}
