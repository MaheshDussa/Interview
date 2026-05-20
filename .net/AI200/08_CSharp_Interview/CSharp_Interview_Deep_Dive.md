# C# Interview — Explanatory Deep Dive

> A senior-level walkthrough of the C# topics that come up in real interviews. Each section is written **explanatorily** — not just "what" but **why**, **how**, **when**, and **what goes wrong**. Targets C# 12 / .NET 8–9 (current LTS in 2026).

---

## 1. Value Types vs Reference Types — The Foundation

**What**: Every C# type is either a **value type** (`struct`, `enum`, all primitives like `int`, `bool`, `DateTime`) or a **reference type** (`class`, `interface`, `delegate`, `string`, `record class`).

**Why it matters**: Determines **where the data lives** (stack vs heap), **how assignment works** (copy vs alias), and **how equality works** (bitwise vs reference).

```csharp
int a = 5;
int b = a;       // b is an independent COPY
b = 10;          // a is still 5

var list1 = new List<int> { 1, 2, 3 };
var list2 = list1;   // both variables point to the SAME object
list2.Add(4);        // list1.Count is now 4 too
```

**Common interview trick**: `string` is a reference type but **behaves like a value** because it's **immutable** — you can never mutate the underlying memory, so aliasing doesn't bite you.

**Boxing/Unboxing**: When you assign a value type to `object` or an interface variable, the runtime allocates a heap object and copies the value into it (**boxing**). Unboxing extracts it back. This is **slow** and creates GC pressure — avoid in hot paths.

```csharp
int i = 42;
object o = i;       // BOX — heap allocation
int j = (int)o;     // UNBOX — type check + copy
```

> **Senior point**: Modern C# avoids boxing with generics (`List<T>` not `ArrayList`), `Span<T>`, and `in`/`ref` parameters. If you see boxing in a profiler, it's a code smell.

---

## 2. Stack vs Heap — Where Things Actually Live

The simple model is "value types on stack, reference types on heap", but **it's more nuanced**:

- A `class` instance is **always** on the heap.
- A `struct` is on the **stack only if it's a local variable**. If it's a **field of a class**, it lives **inside that class's heap object**. If it's **boxed** or **captured by a lambda**, it's on the heap too.
- The **reference** to a heap object (the pointer) is itself on the stack when it's a local.

**Why it matters**: Stack allocations are essentially free (move a pointer). Heap allocations cost time + create GC pressure. Senior devs minimize allocations in hot paths using `Span<T>`, `stackalloc`, `ArrayPool<T>`, struct enumerators, etc.

---

## 3. `string` Deep Dive

`string` in .NET is:
- **Reference type**
- **Immutable** — every "modification" creates a new instance
- **Interned** — string literals are reused (`"abc" == "abc"` reference-equal due to interning)
- **UTF-16** internally (2 bytes per char usually)

```csharp
string s = "hello";
s += " world";   // creates a NEW string; old one is garbage
```

**Concatenation in a loop** = quadratic memory use. Use **`StringBuilder`** for >5 concatenations, or `string.Join`, or **interpolated string handlers** (C# 10+) which avoid allocations when the result isn't needed.

**`string.Equals` vs `==`**:
- For `string`, `==` is overloaded to call `Equals` (ordinal comparison). They're identical.
- For other reference types, `==` is reference equality unless overloaded.
- Always pass `StringComparison.Ordinal` (or `OrdinalIgnoreCase`) when you don't need culture-aware compare — it's **5–10× faster** and avoids subtle bugs (Turkish-I problem).

---

## 4. `class` vs `struct` vs `record` — When to Use Which

| Type | Semantics | Equality | Heap/Stack | Use For |
|---|---|---|---|---|
| `class` | Reference | Reference (default) | Heap | Most objects, services, mutable entities |
| `struct` | Value | Field-by-field (override) | Stack/inline | Small (<16 bytes), immutable, math-like |
| `record class` | Reference | **Value-based by default** | Heap | DTOs, immutable models |
| `record struct` | Value | Value-based | Stack/inline | Small immutable value objects (Money, Point) |

A `record` is **syntactic sugar** for a class with: auto-generated `Equals`/`GetHashCode` based on properties, `ToString` that prints them, `with` expression for non-destructive mutation, and primary constructor parameters.

```csharp
public record Person(string Name, int Age);

var p1 = new Person("Ada", 30);
var p2 = p1 with { Age = 31 };   // new instance; p1 unchanged
p1 == new Person("Ada", 30);     // true — value equality
```

> **Senior point**: Default to `record` for DTOs and immutable models. Use `class` for services and entities with identity (DB rows). Use `struct` only when profiler tells you to.

---

## 5. The Garbage Collector — What You Need to Know

.NET uses a **generational, mark-and-sweep, compacting GC**:

- **Gen 0**: new short-lived objects. Collected often, fast.
- **Gen 1**: survivors of Gen 0. Buffer between short and long-lived.
- **Gen 2**: long-lived objects. Collected rarely; full GC is expensive.
- **LOH (Large Object Heap)**: objects ≥ 85,000 bytes. Not compacted by default → fragmentation.

**Workstation GC vs Server GC**: Server GC (default in ASP.NET Core) uses one heap per core and is faster for throughput. Set in `csproj`: `<ServerGarbageCollection>true</ServerGarbageCollection>`.

**`IDisposable` and `using`**: GC doesn't know about unmanaged resources (file handles, sockets, DB connections). You must release them deterministically.

```csharp
using var conn = new SqlConnection(cs);   // C# 8 using declaration
// auto-disposed at end of scope
```

**The Dispose Pattern** (only needed for unmanaged resources or sealed classes wrapping them):

```csharp
public sealed class MyResource : IDisposable
{
    private bool _disposed;
    private IntPtr _handle;

    public void Dispose()
    {
        if (_disposed) return;
        // free unmanaged...
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    ~MyResource() => Dispose();   // finalizer as safety net
}
```

> **Senior point**: Don't write finalizers unless you own unmanaged handles. Prefer `SafeHandle`. Calling `GC.Collect()` in production is almost always wrong — let the GC do its job.

---

## 6. Boxing, `in`, `ref`, `out`, `ref readonly`, `ref struct`

These keywords are about **how parameters are passed and stored**:

- `ref` — pass by reference (caller and callee see the same variable; either can write).
- `out` — like `ref` but **must be assigned** by the method; caller doesn't need to initialize.
- `in` — pass by **readonly reference**. Avoids copying large structs without allowing mutation.
- `ref readonly` — return a readonly reference.
- `ref struct` (e.g. `Span<T>`) — **stack-only** type; can't be a field of a class, can't be captured by lambda, can't be `await`ed across. Enables zero-copy slicing.

```csharp
public void Process(in LargeStruct s) { /* read s, can't modify */ }
public bool TryParse(string input, out int result) { ... }
```

**Why this matters**: Performance-critical .NET code uses `Span<T>` and `in` parameters to eliminate copies and allocations. Knowing these is a senior-level signal.

---

## 7. `async`/`await` — Really Understanding It

`async`/`await` is **NOT multithreading**. It's a **state machine** the compiler generates so your code can pause at I/O without blocking a thread.

**Mental model**: When you `await` a Task that isn't complete:
1. The method **returns** to its caller, returning a Task that represents "future completion".
2. The local state is saved in a heap-allocated state machine.
3. When the underlying I/O completes (kernel callback), the continuation is **scheduled** to run — possibly on a thread pool thread.

**Why it's a win**: I/O-bound work releases the thread back to the pool. A server with 200 threads can handle 100,000 concurrent requests if they're mostly waiting on I/O.

### Cardinal Sins

1. **`async void`** — fire-and-forget; exceptions crash the process. Only OK for event handlers.
2. **`.Result` / `.Wait()`** — synchronous block on async code. Causes **deadlocks** in legacy SyncContext (WinForms/WPF/old ASP.NET) and wastes a thread in modern ASP.NET Core.
3. **Forgetting to await** — fire-and-forget; exceptions swallowed. Compiler warns with CS4014.
4. **`Task.Run` for I/O** — pointless; you're just moving the wait to a thread pool thread.

### `ConfigureAwait(false)`

In **library** code, use `await task.ConfigureAwait(false)` so the continuation doesn't try to resume on the original SynchronizationContext. In **application** code (ASP.NET Core, console apps), it doesn't matter — they have no SyncContext.

### `ValueTask<T>`

A struct alternative to `Task<T>` to avoid allocation **when the result is often already available** (caching). Don't use it by default — its rules are stricter (don't await twice, don't `.Result` on incomplete one).

### `IAsyncEnumerable<T>`

Async streams — `await foreach` over a producer that yields items as they arrive (paged HTTP results, DB cursors).

```csharp
await foreach (var item in GetPagesAsync(ct))
{
    process(item);
}
```

> **Senior point**: Always propagate `CancellationToken`. Don't catch and swallow `OperationCanceledException`. Use `Task.WhenAll` for parallel awaits, `Parallel.ForEachAsync` for bounded parallelism.

---

## 8. LINQ — How It Actually Works

LINQ has two flavors:
- **LINQ-to-Objects** — works on `IEnumerable<T>` in memory.
- **LINQ-to-Providers** (EF Core, etc.) — translates expression trees to SQL/other.

**Deferred execution**: `Where`, `Select`, etc. return an iterator; nothing runs until you enumerate (`foreach`, `ToList`, `Count`, `First`). This is a constant interview question.

```csharp
var q = list.Where(x => x > 5);   // nothing runs yet
list.Add(100);                    // 100 may now be in the result
foreach (var x in q) ...          // executes NOW
```

**Multiple enumeration** of a deferred query re-runs the query. Materialize with `.ToList()` when you'll enumerate twice or when the source might change.

**Method syntax vs Query syntax**: Identical capability; method syntax is far more common.

**Performance**:
- `Any()` is faster than `Count() > 0` (stops at first match).
- `FirstOrDefault()` over a `List` is O(1) on first elem; over a query, executes the chain.
- LINQ has overhead — in hot loops, a `for` loop is faster.
- .NET 9 added massive LINQ perf improvements; assume LINQ is fast enough until profiled.

**LINQ to EF Core**: Expression trees are translated to SQL. **Client evaluation** is mostly disabled — if EF can't translate, it throws. Watch for `IEnumerable` vs `IQueryable` cliff — calling `ToList()` early forces in-memory, killing perf.

---

## 9. Generics, Covariance, Contravariance

Generics give you **type-safe reusable code without boxing**.

```csharp
public class Repo<T> where T : class, IEntity, new() { ... }
```

**Constraints**: `where T : class`, `struct`, `new()`, `IFoo`, `BaseClass`, `unmanaged`, `notnull`, `T : U`.

**Variance** (only on interfaces and delegates):
- `out T` — **covariant**: `IEnumerable<Dog>` → `IEnumerable<Animal>`. T can only appear in output positions.
- `in T` — **contravariant**: `IComparer<Animal>` → `IComparer<Dog>`. T can only appear in input positions.

```csharp
IEnumerable<string> strings = new List<string>();
IEnumerable<object> objects = strings;   // OK — IEnumerable<out T>
```

> **Senior point**: Arrays are *unsafely* covariant (`string[]` → `object[]`) — assigning a wrong type throws `ArrayTypeMismatchException` at runtime. Use `IEnumerable<T>` or `IReadOnlyList<T>` instead.

---

## 10. Delegates, Events, Lambdas, Func/Action

**Delegate** = type-safe function pointer. `Action` (no return), `Func<...,TResult>` (returns), `Predicate<T>` (returns bool).

```csharp
Func<int, int, int> add = (a, b) => a + b;
int result = add(2, 3);
```

**Lambdas** can **capture variables** from the enclosing scope. The compiler hoists captured variables into a generated closure class on the heap → allocation. In hot paths, prefer static lambdas (`static (x) => ...`, C# 9+) or method groups.

**Events** are delegates with restricted access (only `+=` / `-=` from outside). Subscribe weakly or unsubscribe to prevent **memory leaks** — a long-lived publisher keeping a reference to a short-lived subscriber is a classic leak.

```csharp
public event EventHandler<MyArgs> Changed;
protected virtual void OnChanged(MyArgs e) => Changed?.Invoke(this, e);
```

---

## 11. Exceptions — Best Practices

**Throw on exceptional conditions**, not for control flow.

```csharp
try { ... }
catch (FileNotFoundException ex) when (ex.FileName.EndsWith(".log"))
{
    // exception filter — checked WITHOUT unwinding the stack
}
catch (Exception ex) when (Log(ex))   // common pattern for logging
{
    throw;   // preserves original stack trace
}
```

- `throw;` rethrows preserving stack; `throw ex;` resets it (bug).
- Don't catch `Exception` unless you log + rethrow or are at a top-level boundary.
- `try/finally` (or `using`) for cleanup.
- Custom exceptions: derive from `Exception`, add constructors, no need to mark `[Serializable]` in modern .NET.
- `AggregateException` from `Task.WhenAll` — flatten with `.Flatten().InnerExceptions`.

---

## 12. `IEnumerable<T>` vs `ICollection<T>` vs `IList<T>` vs `IReadOnlyList<T>`

```
IEnumerable<T>     — forward, lazy, sequence
 └─ ICollection<T> — adds Count, Add, Remove
     └─ IList<T>   — adds indexed access
IReadOnlyCollection<T>   — Count + iterate
 └─ IReadOnlyList<T>     — + indexed access
```

Choose the **most restrictive** interface in your API surface. Return `IReadOnlyList<T>` from getters; accept `IEnumerable<T>` if you only need to iterate.

---

## 13. Pattern Matching (C# 8–12)

Massive interview topic now. Patterns are everywhere:

```csharp
// type pattern
if (obj is string s) { ... }

// property pattern
if (person is { Age: > 18, Name.Length: > 0 }) { ... }

// switch expression
string label = shape switch
{
    Circle { Radius: > 10 } => "big circle",
    Square s when s.Side > 0 => $"square {s.Side}",
    null => "none",
    _ => "other"
};

// list pattern (C# 11)
int[] arr = [1, 2, 3];
if (arr is [1, .., 3]) { ... }

// relational + logical
return x is > 0 and < 100 or -1;
```

**Why**: Replaces brittle `if/else` chains, exhaustive (compiler warns on missing case for enums + sealed hierarchies), reads like specification.

---

## 14. Nullable Reference Types (NRT)

Enabled by `<Nullable>enable</Nullable>` in csproj. Now `string` means "not null", `string?` means "may be null". Compiler tracks **flow analysis**.

```csharp
public string? FindName(int id);   // may return null

var n = FindName(1);
// Console.WriteLine(n.Length);   // CS8602 warning
if (n is not null) Console.WriteLine(n.Length);   // OK
```

**Attributes** refine analysis: `[NotNull]`, `[MaybeNull]`, `[NotNullWhen(true)]`, `[MemberNotNull(nameof(_x))]`.

`!` is the **null-forgiving operator** — tells compiler "trust me". Use sparingly; document why.

---

## 15. Dependency Injection in .NET

Built into `Microsoft.Extensions.DependencyInjection`. Three lifetimes:

| Lifetime | Created | Use For |
|---|---|---|
| **Singleton** | Once per app | Stateless config, caches, factories |
| **Scoped** | Once per scope (per HTTP request in ASP.NET Core) | DbContext, per-request services |
| **Transient** | Every resolve | Lightweight stateless services |

**Captive dependency**: A singleton holding a scoped/transient service caches it forever, breaking lifetime guarantees. The DI container in development mode catches this — keep that on.

```csharp
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
```

Constructor injection is the canonical pattern. Avoid service locator (`provider.GetService<T>()` inside methods) — hides dependencies.

---

## 16. Threading & Concurrency

- **`lock`** — easy mutual exclusion (compiles to `Monitor.Enter/Exit`). Always lock on a **private readonly object**, never on `this`, `typeof()`, or strings.
- **`Interlocked`** — atomic ops on ints/longs (`Increment`, `CompareExchange`). No lock needed for simple counters.
- **`SemaphoreSlim`** — async-friendly throttle (`await sem.WaitAsync()`).
- **`Channel<T>`** — high-perf producer/consumer.
- **Concurrent collections** — `ConcurrentDictionary<K,V>`, `ConcurrentQueue<T>`, `ConcurrentBag<T>`.
- **`Parallel.For/ForEachAsync`** — data parallelism (CPU-bound).
- **`Task.WhenAll`** — wait for many; **`Task.WhenAny`** — first to complete.

**Race conditions** happen when shared state is read+written without synchronization. **Deadlocks** happen with circular lock acquisition order.

> **Senior point**: Prefer **immutability** + message passing (Channels, Dataflow) over shared mutable state.

---

## 17. Memory & Performance

Tools every senior should know:
- **BenchmarkDotNet** — micro-benchmarking
- **dotnet-counters / dotnet-trace / dotnet-dump** — production diagnostics
- **PerfView / Visual Studio Profiler** — deep analysis
- **JetBrains dotMemory / dotTrace** — UI-driven

Patterns:
- **`Span<T>` / `Memory<T>`** — zero-copy slicing.
- **`ArrayPool<T>.Shared.Rent` / `Return`** — pooled buffers.
- **`StringBuilder.GetPooledStringBuilder()`** (via libs) — pool builders.
- **`stackalloc`** — stack-allocated array (small sizes).
- **`[SkipLocalsInit]`** — skip zeroing locals (advanced).

Avoid:
- Allocations in tight loops.
- LINQ chains in hot paths.
- Boxing (use generics).
- Recreating regexes — cache them or use `[GeneratedRegex]` (C# 11+).

---

## 18. EF Core — Interview Hot Spots

- **Tracking vs No-Tracking**: `AsNoTracking()` for read-only queries — way faster.
- **N+1 queries**: lazy loading or accessing nav props in loop. Fix with `.Include()` / `.ThenInclude()` or projections.
- **`AsSplitQuery()`** — break a Cartesian explosion from multiple `Include`.
- **DbContext lifetime** — Scoped in DI; NOT thread-safe; never `static`.
- **Migrations** — `dotnet ef migrations add Init` → `dotnet ef database update`. Keep migrations small and reversible.
- **Compiled queries** — `EF.CompileAsyncQuery` for hot queries.
- **Bulk ops** — `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (EF 7+) for set-based updates.
- **Concurrency** — `[Timestamp] byte[] RowVersion`; catch `DbUpdateConcurrencyException`.

---

## 19. ASP.NET Core Pipeline & Middleware

Request flow: kernel → Kestrel → middleware pipeline → endpoint → response back through middleware.

**Middleware**: ordered list of components, each calls `await _next(context)` or short-circuits.

```csharp
app.Use(async (ctx, next) =>
{
    var sw = Stopwatch.StartNew();
    await next();
    log.LogInformation("{path} took {ms}ms", ctx.Request.Path, sw.ElapsedMilliseconds);
});
```

Order matters. Typical order: `UseExceptionHandler` → `UseHttpsRedirection` → `UseStaticFiles` → `UseRouting` → `UseCors` → `UseAuthentication` → `UseAuthorization` → `MapControllers`.

**Filters** (MVC): another layer inside the endpoint — Authorization, Resource, Action, Exception, Result. Use middleware for cross-cutting, filters for MVC-specific.

**Minimal APIs vs Controllers**: Minimal APIs are great for small/microservices; Controllers shine with model binding, filters, conventions for large apps.

---

## 20. SOLID, DI, and Clean Architecture

- **S**RP — single reason to change.
- **O**pen/closed — extend via interfaces/composition, not modification.
- **L**iskov — subtype must honor base contract.
- **I**nterface segregation — many small interfaces > one fat one.
- **D**ependency inversion — depend on abstractions, not concretions.

**Clean / Hexagonal / Onion** all share: **inner core** (domain, no deps) → **application** (use cases) → **infrastructure** (EF, HTTP, queues) → **interface** (API, UI). Inner doesn't know outer. Dependency injection wires it.

> **Senior point**: Don't over-architect. A 3-class CRUD service doesn't need Clean Architecture. Match complexity to problem.

---

## 21. Common Senior-Level Interview Questions (with how to answer)

**Q1. What happens when you "new" a class?**
1. Heap memory allocated (size of fields + sync block + method table pointer).
2. Fields zero-initialized.
3. Constructor chain runs (base first).
4. Reference returned to caller.

**Q2. Difference between `string` and `StringBuilder`?**
`string` is immutable — every change allocates. `StringBuilder` mutates an internal buffer — O(n) for n concatenations vs O(n²).

**Q3. How does `using` work?**
Compiler wraps in `try/finally` and calls `Dispose()` in the finally. `using var x = ...` (C# 8) disposes at the end of the enclosing scope.

**Q4. `Task` vs `Thread`?**
`Thread` is an OS thread (~1 MB stack). `Task` is a work unit scheduled by the thread pool — many tasks share few threads. Use `Task` for async work; `Thread` almost never directly.

**Q5. Why is `async void` bad?**
Exceptions can't be observed — they go to `SynchronizationContext` or crash the process. You can't `await` it. Only use for event handlers.

**Q6. What does `volatile` do?**
Tells the JIT/CPU not to reorder reads/writes of this field and not to cache it in a register. Rarely correct — prefer `Interlocked` or `lock`.

**Q7. Reference equality vs value equality?**
`ReferenceEquals(a, b)` — same object. `a.Equals(b)` — semantic equality (overridable). `==` — overloadable; for strings & records does value, for classes defaults to reference.

**Q8. How does `Dictionary<K,V>` work?**
Open-addressing hash table with separate chaining via linked entries. `GetHashCode` finds bucket, `Equals` resolves collisions. Bad hash codes → O(n) lookups.

**Q9. Difference between `IEnumerable` and `IQueryable`?**
`IEnumerable` executes in memory. `IQueryable` builds an expression tree the provider (EF Core) translates to SQL. Crossing the boundary (`ToList`) forces evaluation.

**Q10. How do you avoid memory leaks in C#?**
- Unsubscribe from long-lived events.
- Dispose `IDisposable` (use `using`).
- Don't hold static references to large objects.
- Watch for closures capturing `this` in long-lived callbacks.
- Profile with dotMemory / SoS.

**Q11. What's a `Span<T>`?**
A stack-allocated struct giving a typed view over contiguous memory (array, stackalloc, native). Enables zero-copy slicing for parsing/serialization. `ref struct` — limited lifetime.

**Q12. `record` vs `class` — when do you choose?**
`record` for immutable data with value equality (DTOs, configs, events). `class` for behavior-rich, identity-based, mutable entities (services, aggregates).

**Q13. How does ASP.NET Core authentication work?**
`UseAuthentication` invokes the configured scheme (Cookies, JWT Bearer, OIDC). Scheme parses the token/cookie, builds a `ClaimsPrincipal`, attaches it to `HttpContext.User`. `UseAuthorization` then checks policies/roles.

**Q14. What's the difference between authentication and authorization?**
Authentication = "who are you?" (identity verification). Authorization = "what can you do?" (permission check). Order in pipeline: `UseAuthentication` before `UseAuthorization`.

**Q15. Explain `IDisposable` vs finalizer.**
`IDisposable.Dispose()` is **deterministic** — caller controls timing. Finalizer (`~ClassName`) runs **non-deterministically** by GC, on a dedicated thread, only for unmanaged resources. Modern guidance: use `SafeHandle`, you rarely need a finalizer.

---

## 22. The "Code Review" Interview — What They Look For

When given code on a whiteboard or in a take-home, seniors are evaluated on:

| They watch for | Bad answer | Good answer |
|---|---|---|
| Null handling | NRE on every line | NRTs enabled; guard at boundaries |
| Async correctness | `.Result`, `async void` | `async Task`, `ConfigureAwait` (libs), `CancellationToken` everywhere |
| Resource cleanup | Forgot `Dispose` | `using` for every `IDisposable` |
| Allocations | LINQ in hot loop | `Span<T>`, pooling, struct enumerators |
| Concurrency | Lock on `this` | Private lock object; `Interlocked`/channels |
| Tests | "It works on my machine" | xUnit + FluentAssertions + property tests |
| Logging | `Console.WriteLine` | `ILogger<T>` with structured templates |
| Security | String concat SQL | Parameterized queries / EF |

---

## 23. .NET 8 / 9 Features Worth Mentioning

- **Native AOT** — compile to native, no JIT, tiny container images.
- **`required` members** — must be set on init.
- **`primary constructors` on classes** (C# 12).
- **Collection expressions** `[1, 2, 3]`.
- **Frozen collections** (`FrozenDictionary`, `FrozenSet`) — read-optimized.
- **`TimeProvider`** — abstraction for time (test-friendly).
- **Keyed services** — `AddKeyedSingleton`.
- **`[GeneratedRegex]`** — compile-time regex.
- **Channels / TimerProvider / IHostedService** — robust hosting primitives.
- **Source generators** — `System.Text.Json`, `LoggerMessage`, `RegexGenerator`, `JsonSerializerContext` for AOT.

---

## 24. Cheat Sheet Answers

| Question | One-liner |
|---|---|
| ref vs out | both pass by ref; out **must** be assigned in method |
| const vs readonly | const = compile-time literal; readonly = run-time, set in ctor |
| static class | sealed + abstract; only static members; no instances |
| sealed | can't be inherited (enables JIT optimizations) |
| virtual / override / new | virtual = overridable; override = overrides base; new = hides base |
| abstract class vs interface | abstract can hold state + impl; interface is contract (+ default methods C# 8+) |
| explicit vs implicit interface | explicit hides member unless cast to interface |
| ?. and ?? | null-conditional access; null-coalescing |
| ?[] | null-conditional index `arr?[0]` |
| `is` vs `as` | is = check; as = cast or null |
| Equality | `==` ref by default; override `Equals` + `GetHashCode` together |
| `default(T)` | zero for value types, null for reference |
| GC.SuppressFinalize | call after Dispose to skip finalizer |

---

## 25. Mental Model

> **Senior C# = (1) deep memory/lifetime understanding, (2) correct async + cancellation, (3) generics + variance fluency, (4) modern pattern matching, (5) DI + clean architecture, (6) measured performance with profilers, (7) NRT-aware safe code, (8) knows when to drop to `Span<T>` / `Channel<T>` / source generators. Interviewers don't want trivia — they want to see you reason about trade-offs.**
