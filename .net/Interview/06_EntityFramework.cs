// =====================================================================
//  06) ENTITY FRAMEWORK CORE — Q&A and Scenarios
// =====================================================================
namespace Interview.EFCore
{
    // ---------------------------------------------------------------------
    // Q1: What is EF Core?
    // A : Lightweight, cross-platform ORM. Maps C# classes to relational
    //     tables. Supports LINQ, migrations, change tracking.
    // ---------------------------------------------------------------------

    // Q2: Code-first vs Database-first?
    // A : Code-first - classes drive schema via migrations (preferred).
    //     DB-first   - scaffold classes from existing DB
    //                  (dotnet ef dbcontext scaffold ...).

    // Q3: DbContext lifetime?
    // A : Scoped (per request) by default. Not thread-safe; one instance
    //     per logical operation. Use IDbContextFactory for parallel work.

    // ---------------------------------------------------------------------
    // Q4: Tracked vs No-tracking queries?
    // A : Tracked     - change tracker watches entities (default).
    //     NoTracking  - read-only, faster, lower memory.
    //     Use .AsNoTracking() for queries you won't update.
    // ---------------------------------------------------------------------

    // Q5: Eager / Lazy / Explicit loading?
    // A : Eager    - .Include(o => o.Items)
    //     Lazy     - proxies load on access (requires Microsoft.EntityFrameworkCore.Proxies)
    //     Explicit - context.Entry(o).Collection(x => x.Items).Load();

    // Q6: The N+1 problem?
    // A : Loading parents then accessing children in a loop -> 1 + N queries.
    //     Fix: .Include(...) or projection (.Select(o => new { o.Items }))

    // ---------------------------------------------------------------------
    // Q7: How to do migrations?
    // A : dotnet ef migrations add InitialCreate
    //     dotnet ef database update
    //     dotnet ef migrations remove
    //     dotnet ef migrations script --idempotent  // safe for prod deploys
    // ---------------------------------------------------------------------

    // Q8: Fluent API vs Data Annotations?
    // A : Annotations are simple ([Key], [Required], [MaxLength(50)]).
    //     Fluent API in OnModelCreating handles complex mappings
    //     (composite keys, many-to-many, conversions, indexes).

    // Q9: Many-to-many in EF Core 5+?
    // A : Auto-creates join table without an explicit class:
    //     public ICollection<Tag> Tags { get; set; }    on both sides.

    // Q10: How to handle concurrency?
    // A : Add a byte[] RowVersion property with [Timestamp] (SQL Server) or
    //     ConcurrencyToken in Fluent API. On conflict, catch
    //     DbUpdateConcurrencyException and decide (client wins / merge).

    // Q11: Transactions?
    // A : SaveChanges() is already a transaction. For multi-context work:
    //     using var tx = ctx.Database.BeginTransaction(); ... tx.Commit();

    // Q12: SaveChanges vs SaveChangesAsync?
    // A : Always prefer async in server code to free thread pool.

    // Q13: Difference between Find, FirstOrDefault, Single?
    // A : Find  - checks change tracker first, then DB; only by PK.
    //     FirstOrDefault - DB query, returns first.
    //     Single - DB query, expects exactly one.

    // Q14: Raw SQL with EF?
    // A : ctx.Set<Order>().FromSqlInterpolated($"SELECT * FROM Orders WHERE Id={id}")
    //     Interpolated forms parameterize automatically (avoid SQL injection).

    // Q15: Global query filters?
    // A : Soft delete / multitenancy:
    //     modelBuilder.Entity<Order>().HasQueryFilter(o => !o.IsDeleted);
    //     Bypass with .IgnoreQueryFilters().

    // ---------------------------------------------------------------------
    // [Scenario] Q16: Performance degrades over time in a long-running job.
    // A : DbContext accumulates tracked entities. Use AsNoTracking, batch
    //     SaveChanges + dispose context periodically.
    // ---------------------------------------------------------------------

    // [Scenario] Q17: You see "The instance of entity type 'X' cannot be
    //   tracked because another instance with the same key is already tracked."
    // A : Two entities with the same key are attached. Detach one, or use
    //     .AsNoTracking() / re-fetch then update.

    // [Scenario] Q18: A page lists orders with their customer; query is slow.
    // A : Likely N+1. Add .Include(o => o.Customer) or project DTO with .Select().

    // [Scenario] Q19: Migration fails on prod with "table already exists".
    // A : Migrations history out of sync. Use idempotent SQL script
    //     (dotnet ef migrations script --idempotent) or sync __EFMigrationsHistory.

    // [Scenario] Q20: Bulk insert 1M rows is slow.
    // A : EF Core 7+ has ExecuteUpdate/ExecuteDelete (server-side). For inserts
    //     use libraries (EFCore.BulkExtensions, Z.EntityFramework.Extensions)
    //     or SqlBulkCopy.

    // [Scenario] Q21: How do you avoid lazy loading in APIs?
    // A : Don't return entities directly. Project to DTOs in .Select(...)
    //     and disable lazy loading (don't install proxies package).

    internal static class _Ef { }
}
