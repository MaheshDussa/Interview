/* =============================================================================
   10_Indexes_And_Execution_Plans.sql
   -----------------------------------------------------------------------------
   Indexes and execution plans are the #1 SQL Server performance topic. This
   script covers every flavor of index, the structures behind them, and how
   to read what the optimizer is actually doing.

   Plain-English mental model:
     * HEAP            = unordered pile of pages (no clustered index).
     * CLUSTERED IX    = the table itself, physically ordered by the key.
                         ONE per table. The clustering key is appended to
                         every NONCLUSTERED index row (so keep it narrow!).
     * NONCLUSTERED IX = a separate B-tree pointing to the heap RID or the
                         clustered key. Many per table.
     * COVERING IX     = a NC index that already contains every column the
                         query needs -> NO key lookup required.

   Interview talking points:
     * "Narrow, unique, static, ever-increasing" is the textbook clustered
       key (e.g. IDENTITY INT). UUIDs as clustered keys cause page splits.
     * INCLUDE columns are stored at the LEAF only - they widen the leaf
       but not the upper B-tree levels.
     * Filtered indexes shrink hot subsets (e.g. only WHERE IsActive = 1).
     * Composite index column order matters: leftmost prefix wins.
     * "SARGable" = predicate can use an index. Wrapping the column in a
       function (e.g. UPPER(Email) = ...) usually kills SARGability.
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- DEMO TABLE
-- =============================================================================
IF OBJECT_ID('dbo.OrderHeader', 'U') IS NOT NULL DROP TABLE dbo.OrderHeader;
CREATE TABLE dbo.OrderHeader
(
    OrderId     INT IDENTITY(1,1) NOT NULL,    -- narrow, ever-increasing -> good clustered key
    CustomerId  INT           NOT NULL,
    OrderDate   DATETIME2     NOT NULL,
    Status      VARCHAR(20)   NOT NULL,
    TotalAmount DECIMAL(12,2) NOT NULL,
    Notes       NVARCHAR(MAX) NULL,
    CONSTRAINT PK_OrderHeader PRIMARY KEY CLUSTERED (OrderId)
);
GO

-- Seed 50,000 rows so the optimizer chooses index vs scan realistically.
;WITH n AS
(
    SELECT TOP (50000) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS n
    FROM   sys.all_objects a CROSS JOIN sys.all_objects b
)
INSERT INTO dbo.OrderHeader (CustomerId, OrderDate, Status, TotalAmount)
SELECT (n % 1000) + 1,
       DATEADD(MINUTE, -n, SYSUTCDATETIME()),
       CASE n % 4 WHEN 0 THEN 'Open' WHEN 1 THEN 'Shipped' WHEN 2 THEN 'Closed' ELSE 'Cancelled' END,
       CAST((n % 9999) AS DECIMAL(12,2))
FROM   n;
GO


-- =============================================================================
-- 1) NONCLUSTERED INDEX  - basic single-column
-- =============================================================================
CREATE NONCLUSTERED INDEX IX_OrderHeader_CustomerId
    ON dbo.OrderHeader (CustomerId);
GO


-- =============================================================================
-- 2) COMPOSITE INDEX + INCLUDE  - covering common queries
-- =============================================================================
-- Key columns are used for seek/range. INCLUDE columns ride along at the
-- leaf so the index "covers" the query (no key lookup back to the table).
CREATE NONCLUSTERED INDEX IX_OrderHeader_Cust_Date
    ON dbo.OrderHeader (CustomerId, OrderDate DESC)     -- column ORDER matters!
    INCLUDE (Status, TotalAmount);
GO

-- The query below is COVERED by IX_OrderHeader_Cust_Date.
SELECT OrderId, Status, TotalAmount
FROM   dbo.OrderHeader
WHERE  CustomerId = 42
  AND  OrderDate >= '2026-01-01';


-- =============================================================================
-- 3) FILTERED INDEX  - smaller, faster for a hot subset
-- =============================================================================
CREATE NONCLUSTERED INDEX IX_OrderHeader_OpenOnly
    ON dbo.OrderHeader (OrderDate)
    INCLUDE (CustomerId, TotalAmount)
    WHERE Status = 'Open';
GO

-- Only queries whose predicate matches the filter benefit:
SELECT TOP (100) *
FROM   dbo.OrderHeader
WHERE  Status = 'Open'                              -- must match the filtered predicate
  AND  OrderDate >= DATEADD(DAY, -7, SYSUTCDATETIME())
ORDER  BY OrderDate DESC;


-- =============================================================================
-- 4) UNIQUE / PRIMARY KEY INDEXES
-- =============================================================================
-- UNIQUE NONCLUSTERED enforces uniqueness AND speeds equality lookups.
-- Note: NULLs are allowed at most once in a UNIQUE constraint (SQL Server).
CREATE UNIQUE NONCLUSTERED INDEX UX_OrderHeader_Notes_Hash
    ON dbo.OrderHeader (TotalAmount)
    WHERE TotalAmount > 0;
GO


-- =============================================================================
-- 5) COLUMNSTORE INDEX  - analytics workloads (covered more in script 13)
-- =============================================================================
-- A NONCLUSTERED COLUMNSTORE allows OLTP + analytics on the same table.
CREATE NONCLUSTERED COLUMNSTORE INDEX NCCI_OrderHeader
    ON dbo.OrderHeader (CustomerId, OrderDate, Status, TotalAmount);
GO


-- =============================================================================
-- 6) EXECUTION PLANS  - how to read what SQL Server does
-- =============================================================================
-- Toggles:
SET STATISTICS IO   ON;     -- logical/physical reads per object
SET STATISTICS TIME ON;     -- CPU + elapsed
-- Plus, in SSMS:
--   Ctrl+M -> Include Actual Execution Plan
--   Ctrl+L -> Display Estimated Execution Plan
--
-- What to look for:
--   * "Clustered Index Scan" vs "Clustered Index Seek" - SEEK is targeted.
--   * "Key Lookup" -> indicates a NON-covering NC index; consider INCLUDE.
--   * Thick arrows = many rows; mismatch between Estimated/Actual rows ->
--     stale statistics or parameter sniffing.
--   * Spool / Sort / Hash Match (Aggregate) operators = memory grants.

-- Bad: function on the indexed column => non-SARGable => SCAN.
SELECT COUNT(*)
FROM   dbo.OrderHeader
WHERE  CAST(OrderDate AS DATE) = '2026-05-18';

-- Good: rewrite as a half-open range => SEEK.
SELECT COUNT(*)
FROM   dbo.OrderHeader
WHERE  OrderDate >= '2026-05-18'
  AND  OrderDate <  '2026-05-19';

SET STATISTICS IO   OFF;
SET STATISTICS TIME OFF;
GO


-- =============================================================================
-- 7) INDEX MAINTENANCE
-- =============================================================================
-- Fragmentation = page order on disk no longer matches the B-tree order.
-- Rule of thumb:
--   * <  5 %  -> leave alone
--   *  5-30 % -> REORGANIZE (online, lightweight)
--   * > 30 %  -> REBUILD    (heavier; ONLINE = ON keeps the table available
--                            on Enterprise edition / Azure SQL)
--
-- Inspect:
SELECT  OBJECT_NAME(ips.object_id)            AS table_name,
        i.name                                AS index_name,
        ips.index_type_desc, ips.avg_fragmentation_in_percent,
        ips.page_count
FROM    sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'SAMPLED') ips
JOIN    sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE   ips.page_count > 100;

-- Maintain (templates):
-- ALTER INDEX IX_OrderHeader_Cust_Date ON dbo.OrderHeader REORGANIZE;
-- ALTER INDEX ALL ON dbo.OrderHeader REBUILD WITH (ONLINE = ON);


-- =============================================================================
-- 8) MISSING INDEX SUGGESTIONS  - take them as HINTS, not gospel
-- =============================================================================
SELECT TOP (10)
        mid.statement                          AS table_name,
        migs.avg_user_impact, migs.user_seeks, migs.user_scans,
        mid.equality_columns, mid.inequality_columns, mid.included_columns
FROM    sys.dm_db_missing_index_groups mig
JOIN    sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
JOIN    sys.dm_db_missing_index_details mid     ON mig.index_handle       = mid.index_handle
ORDER  BY migs.avg_user_impact DESC;


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * One clustered, many nonclustered. Clustering key piggybacks on every NC.
   * Covering index + INCLUDE -> kill "Key Lookup".
   * Filtered indexes shrink hot subsets.
   * Composite key order: equality cols first, then range cols.
   * Read the plan, check statistics, watch fat arrows, beware estimates.
   ============================================================================= */
