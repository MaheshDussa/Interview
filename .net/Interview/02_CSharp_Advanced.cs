// =====================================================================
//  02) C# ADVANCED — async, LINQ, delegates, generics
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Interview.CSharpAdvanced
{
    // ---------------------------------------------------------------------
    // Q1: Difference between Thread, Task, async/await?
    // A : Thread - OS-level, expensive, blocking.
    //     Task   - unit of work scheduled on the thread pool (or sync).
    //     async/await - syntactic sugar over Task; releases the thread
    //                   during I/O waits, resumes when done.
    // ---------------------------------------------------------------------

    // Q2: What does `await` actually do?
    // A : Splits the method at the await point. If task incomplete,
    //     returns control to caller and schedules a continuation that
    //     resumes when the task finishes (on captured SynchronizationContext).

    // Q3: When is `ConfigureAwait(false)` important?
    // A : In library code or non-UI server code where you don't need to
    //     resume on the original context. Avoids deadlocks and improves perf.
    //     In ASP.NET Core there's no SynchronizationContext, so it matters less.

    // Q4: Why is `async void` dangerous?
    // A : Exceptions can't be awaited/caught; they crash the process.
    //     Allowed ONLY for event handlers.

    // ---------------------------------------------------------------------
    // Q5: Common async pitfall — .Result / .Wait() deadlocks
    public static class AsyncPitfalls
    {
        // BAD in UI/old ASP.NET: deadlocks because continuation needs the
        // captured context that the caller is still holding.
        public static int BadSync() => GetAsync().Result;

        public static async Task<int> GoodAsync() => await GetAsync();

        private static async Task<int> GetAsync()
        {
            await Task.Delay(10);
            return 42;
        }
    }
    // ---------------------------------------------------------------------

    // Q6: Task.Run vs Task.Factory.StartNew?
    // A : Task.Run is the modern, simple API. StartNew has many overloads
    //     that can lead to bugs (e.g., not unwrapping nested tasks).

    // Q7: Parallel.For vs Task.WhenAll?
    // A : Parallel.For is for CPU-bound work (data parallelism).
    //     Task.WhenAll is for awaiting many I/O-bound tasks concurrently.

    // Q8: CancellationToken — why pass it everywhere?
    // A : Cooperative cancellation. Long-running ops should check the
    //     token regularly so callers can abort gracefully.
    public static class Cancel
    {
        public static async Task WorkAsync(CancellationToken ct)
        {
            for (int i = 0; i < 1000; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(1, ct);
            }
        }
    }

    // ---------------------------------------------------------------------
    // Q9: Delegate vs Func vs Action vs Event?
    // A : delegate     - a type that points to a method (compile-time).
    //     Func<...,T>  - built-in generic delegate returning T.
    //     Action<...>  - built-in generic delegate returning void.
    //     event        - pub/sub wrapper around a delegate; only += / -=
    //                    are allowed externally.
    // ---------------------------------------------------------------------
    public static class Delegates
    {
        public static void Demo()
        {
            Func<int, int> square = x => x * x;
            Action<string>  log    = s => Console.WriteLine(s);
            log($"3^2 = {square(3)}");
        }
    }

    // Q10: What is a closure? Capture pitfalls?
    // A : Lambda that captures outer variables. Captures by REFERENCE.
    //     Classic bug: capturing a loop variable inside a closure -> all
    //     closures see the final value. Fix: copy to a local.

    // ---------------------------------------------------------------------
    // Q11: IEnumerable vs IQueryable?
    // A : IEnumerable - LINQ to Objects, executes in memory.
    //     IQueryable  - builds an expression tree, translated by a provider
    //                   (e.g., EF Core -> SQL). Filters execute on the server.
    //     Casting IQueryable to IEnumerable loses server-side translation.
    // ---------------------------------------------------------------------

    // Q12: Deferred vs immediate execution in LINQ?
    // A : Deferred (Where, Select, OrderBy) - runs only when enumerated.
    //     Immediate (ToList, ToArray, Count, First) - runs now.

    // Q13: First vs FirstOrDefault vs Single?
    // A : First   - first match; throws if none.
    //     FirstOrDefault - first or default(T).
    //     Single  - exactly one; throws if 0 or >1.
    public static class LinqQuirks
    {
        public static void Demo()
        {
            var nums = new[] { 1, 2, 3, 4 };
            var evens = nums.Where(n => n % 2 == 0); // deferred
            var count = evens.Count();               // immediate
            Console.WriteLine(count);
        }
    }

    // ---------------------------------------------------------------------
    // Q14: Generics — why?
    // A : Type safety + no boxing + code reuse. List<T> vs ArrayList.
    // ---------------------------------------------------------------------
    public class Repo<T> where T : class, new()
    {
        public T Create() => new T();
    }

    // Q15: Covariance/contravariance (in/out)?
    // A : out T (covariant)    - IEnumerable<Dog> assignable to IEnumerable<Animal>
    //     in  T (contravariant)- Action<Animal> assignable to Action<Dog>

    // ---------------------------------------------------------------------
    // [Scenario] Q16: API call seems slow because it awaits each item in
    //   a loop. How to speed up?
    // A : Kick off all tasks first, then await Task.WhenAll:
    //     var tasks = ids.Select(GetAsync); var results = await Task.WhenAll(tasks);
    // ---------------------------------------------------------------------

    // [Scenario] Q17: A LINQ query against EF Core suddenly fetches the
    //   entire table into memory and filters there. Why?
    // A : Somewhere you called .ToList() / .AsEnumerable() before Where(),
    //     OR you used a method EF can't translate (e.g., a custom func).

    // [Scenario] Q18: You see "Collection was modified" while enumerating.
    //   Fix?
    // A : Don't mutate a collection while iterating. Take a snapshot
    //     (ToList()) or use a concurrent collection.

    // [Scenario] Q19: Why use `IAsyncEnumerable<T>`?
    // A : Streaming async data without buffering the whole sequence.
    //     `await foreach (var item in QueryAsync())` — perfect for paged APIs.

    // [Scenario] Q20: Lock vs SemaphoreSlim in async code?
    // A : `lock` blocks the THREAD and can't be awaited. In async use
    //     `SemaphoreSlim.WaitAsync()` for cross-thread async-safe locking.
}
