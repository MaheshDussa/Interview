// =====================================================================
//  16) SQL / DATABASE — Interview Q&A
// =====================================================================
namespace Interview.SqlDb
{
    // ---------------------------------------------------------------------
    //  FUNDAMENTALS
    // ---------------------------------------------------------------------

    // Q1: SQL vs NoSQL?
    // A : SQL    - relational, schema, ACID, joins (SQL Server, Postgres, MySQL).
    //     NoSQL - document (Mongo, Cosmos), key/value (Redis), column (Cassandra),
    //             graph (Neo4j). Schema-flexible, scale-out, often BASE.

    // Q2: ACID?
    // A : Atomicity, Consistency, Isolation, Durability.
    //     Either the whole transaction commits or none of it.

    // Q3: Normalization — 1NF, 2NF, 3NF (quick).
    // A : 1NF - atomic columns, no repeating groups.
    //     2NF - 1NF + no partial dependency on composite key.
    //     3NF - 2NF + no transitive dependency (non-key on non-key).
    //     Denormalize for read performance in OLAP / reporting.

    // Q4: Primary key vs Unique key vs Foreign key?
    // A : PK  - one per table, NOT NULL, unique, often clustered.
    //     UK  - unique, can be nullable (depending on DB).
    //     FK  - enforces referential integrity to another table's PK/UK.

    // Q5: Clustered vs Non-clustered index?
    // A : Clustered     - table is physically sorted by these keys.
    //                     One per table. PK is clustered by default in SQL Server.
    //     Non-clustered - separate B-tree pointing to rows; many allowed.
    //     Covering index= includes all columns a query needs (no lookup).

    // ---------------------------------------------------------------------
    //  JOINS & QUERIES
    // ---------------------------------------------------------------------

    // Q6: INNER vs LEFT vs RIGHT vs FULL vs CROSS join.
    //   INNER - rows that match in both.
    //   LEFT  - all from LEFT + matches; NULLs on right when no match.
    //   RIGHT - all from right + matches.
    //   FULL  - all rows, NULL on side without match.
    //   CROSS - Cartesian product (every row × every row).

    // Q7: WHERE vs HAVING?
    // A : WHERE  filters rows BEFORE grouping.
    //     HAVING filters AFTER GROUP BY (uses aggregates).

    // Q8: GROUP BY + aggregates.
    //   /// SELECT CustomerId, COUNT(*) AS Orders, SUM(Total) AS Revenue
    //   /// FROM Orders
    //   /// WHERE OrderDate >= '2026-01-01'
    //   /// GROUP BY CustomerId
    //   /// HAVING SUM(Total) > 1000
    //   /// ORDER BY Revenue DESC;

    // Q9: UNION vs UNION ALL?
    // A : UNION     - removes duplicates (extra sort/distinct cost).
    //     UNION ALL - keeps duplicates, FASTER. Use unless you need de-dupe.

    // Q10: Subquery vs CTE vs Derived table?
    // A : All can express the same logic; CTE is the most readable.
    //     CTE can be recursive (good for hierarchies, paths).
    //
    //   /// WITH Top AS (
    //   ///   SELECT TOP 10 * FROM Products ORDER BY Sales DESC
    //   /// )
    //   /// SELECT * FROM Top WHERE Stock > 0;

    // Q11: Window functions?
    //   /// SELECT
    //   ///   Id, CustomerId, Total,
    //   ///   ROW_NUMBER()  OVER (PARTITION BY CustomerId ORDER BY Total DESC) AS RankInCust,
    //   ///   SUM(Total)    OVER (PARTITION BY CustomerId) AS CustTotal
    //   /// FROM Orders;
    //
    // Useful for ranking, running totals, "latest per group", etc.

    // Q12: PIVOT / UNPIVOT?
    // A : Convert rows <-> columns. Often replaced by CASE WHEN aggregations
    //     when columns are known.

    // ---------------------------------------------------------------------
    //  INDEXING & PERFORMANCE
    // ---------------------------------------------------------------------

    // Q13: When does an index NOT help?
    // A : - Functions on the column (WHERE YEAR(d) = 2026).
    //     - Implicit conversions (nvarchar param vs varchar column).
    //     - Low selectivity (column has 2 values).
    //     - SELECT * forces row lookups -> use covering index or projection.

    // Q14: Execution plan — how to read?
    // A : SET STATISTICS IO, TIME ON. Look at: Estimated vs Actual rows
    //     (off by >10x => stats out of date), Key Lookups, Index Scans
    //     (vs Seeks), Hash Joins on huge sets, expensive Sort/Spool ops.

    // Q15: Statistics?
    // A : DB stores distribution of values per column. Auto-updates can lag
    //     under heavy load. UPDATE STATISTICS or rebuild indexes when plans
    //     go bad after big data changes.

    // Q16: Fragmentation?
    // A : Index pages out of order -> more I/O. ALTER INDEX ... REORGANIZE
    //     (<30% frag) or REBUILD (>=30%). Use FILLFACTOR for hot tables.

    // Q17: Locks, blocking, deadlocks?
    // A : Locks coordinate concurrency. Long transactions cause blocking.
    //     Deadlocks (two tx waiting on each other) -> SQL picks a victim
    //     (Error 1205). Mitigate: consistent locking order, smaller tx,
    //     READ COMMITTED SNAPSHOT, retry logic.

    // Q18: Isolation levels (quick recap).
    // A : Read Uncommitted < Read Committed (default) < Repeatable Read
    //         < Serializable. SNAPSHOT uses row versions (readers don't block writers).

    // ---------------------------------------------------------------------
    //  ADMIN / DESIGN
    // ---------------------------------------------------------------------

    // Q19: Stored procedure vs function vs view?
    // A : Procedure - executes statements, may modify data, can have multiple
    //                 result sets, OUT params.
    //     Function  - returns scalar/table, used in SELECT, no side effects.
    //     View      - saved SELECT; indexed views can be materialized.

    // Q20: Trigger — when to use (and avoid)?
    // A : Use for audit, derived data within same DB. Avoid for cross-system
    //     side effects (hard to test, hidden logic). Prefer outbox.

    // Q21: Surrogate vs Natural keys?
    // A : Surrogate (Identity / GUID) - immutable, simple FKs, good default.
    //     Natural (email, ISBN)        - meaningful but can change.
    //     GUID PK + non-clustered, separate clustered key, to avoid page splits.

    // Q22: IDENTITY vs SEQUENCE vs GUID PK?
    // A : IDENTITY  - per-table counter, simple.
    //     SEQUENCE  - shared counter, more control.
    //     GUID      - globally unique, no central allocator; clustering by
    //                 GUID is bad for inserts unless sequential GUIDs.

    // Q23: Partitioning?
    // A : Split a large table by range/list (date, tenant). Faster prune,
    //     easier archival. Requires planning around indexes & stats.

    // Q24: Backup & recovery models (SQL Server)?
    // A : Simple        - no log backups; lose data since last full backup.
    //     Full          - log backups for point-in-time restore.
    //     Bulk-logged   - faster bulk ops, limited PITR.
    //     Test restores, not just backups.

    // ---------------------------------------------------------------------
    //  WITH .NET
    // ---------------------------------------------------------------------

    // Q25: EF Core query with PROJECTION vs entity load?
    // A : .Select(x => new Dto { ... }) generates a thinner SELECT and skips
    //     change tracking -> faster + smaller payload.

    // Q26: Parameter sniffing in SQL + EF?
    // A : First call's parameter value shapes the plan; bad if values differ
    //     wildly (skewed data). Mitigations: OPTION (RECOMPILE), OPTIMIZE FOR,
    //     or EF interceptor adding the hint.

    // Q27: When to use Dapper instead of EF Core?
    // A : Read-heavy hot paths, complex SQL/sprocs, multi-result-set scenarios,
    //     or when EF translation gets in the way. EF for the rest.

    // ---------------------------------------------------------------------
    //  SCENARIOS
    // ---------------------------------------------------------------------

    // [Scenario] Q28: Query is fast in SSMS but slow from the app.
    // A : Parameter sniffing + wrong types (NVARCHAR vs VARCHAR) cause
    //     different plans. Match SqlDbType + Size exactly; add OPTION (RECOMPILE)
    //     if needed.

    // [Scenario] Q29: SELECT COUNT(*) on huge table is slow every page load.
    // A : Cache the count, use approximate counts (sys.dm_db_partition_stats),
    //     or maintain a counter via triggers/outbox.

    // [Scenario] Q30: Need "top 10 per category".
    //   /// SELECT * FROM (
    //   ///   SELECT *, ROW_NUMBER() OVER (PARTITION BY CategoryId ORDER BY Sales DESC) AS rn
    //   ///   FROM Products
    //   /// ) t WHERE rn <= 10;

    // [Scenario] Q31: Find duplicate emails.
    //   /// SELECT Email, COUNT(*) c
    //   /// FROM Users
    //   /// GROUP BY Email
    //   /// HAVING COUNT(*) > 1;

    // [Scenario] Q32: Update with join.
    //   /// UPDATE o
    //   /// SET    o.Status = 'Shipped'
    //   /// FROM   Orders o
    //   /// JOIN   Shipments s ON s.OrderId = o.Id
    //   /// WHERE  s.Status = 'Delivered';

    // [Scenario] Q33: Soft delete vs hard delete?
    // A : Soft delete - IsDeleted=1 + filter; preserves data + FKs.
    //     Hard delete - row gone; cleaner but loses history.
    //     For GDPR: hard delete or pseudonymization.

    // [Scenario] Q34: Schema migration with zero downtime.
    // A : Expand-Contract:
    //     1) Add new column nullable / dual write.
    //     2) Backfill in batches.
    //     3) Switch reads to new column.
    //     4) Stop writing the old column.
    //     5) Drop old column in a later release.

    // [Scenario] Q35: How to find missing indexes?
    // A : sys.dm_db_missing_index_details + sys.dm_db_index_usage_stats.
    //     Vet recommendations — don't blindly create everything.

    // [Scenario] Q36: A report needs data from 5 tables, queries are slow.
    // A : Build an indexed view, materialized table, or read replica fed
    //     by ETL/CDC. Separate OLTP from OLAP.

    internal static class _Sql { }
}
