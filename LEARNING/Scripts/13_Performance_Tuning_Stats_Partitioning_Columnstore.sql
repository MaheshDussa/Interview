/* =============================================================================
   13_Performance_Tuning_Stats_Partitioning_Columnstore.sql
   -----------------------------------------------------------------------------
   Performance topics most often probed in interviews:
       1) Statistics + cardinality estimation
       2) Parameter sniffing + recompile hints
       3) Plan cache & Query Store
       4) Table partitioning (sliding window)
       5) Columnstore indexes (analytics workloads)
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) STATISTICS
-- =============================================================================
-- The optimizer makes cardinality estimates from histograms stored in
-- "statistics" objects (auto-created with indexes, or via auto-stats on
-- predicate columns).
--
-- Inspect:
SELECT s.name AS stat_name, OBJECT_NAME(s.object_id) AS table_name,
       sp.last_updated, sp.rows, sp.rows_sampled, sp.unfiltered_rows, sp.modification_counter
FROM   sys.stats s
CROSS APPLY sys.dm_db_stats_properties(s.object_id, s.stats_id) sp
WHERE  OBJECTPROPERTY(s.object_id, 'IsUserTable') = 1
ORDER  BY sp.modification_counter DESC;

-- Refresh stats (small tables: fine. Big tables: schedule off-hours):
-- UPDATE STATISTICS dbo.USERS WITH FULLSCAN;
-- EXEC sp_updatestats;       -- updates all out-of-date stats in the DB

-- View the histogram + density of a stats object (the "actual" data SQL uses):
-- DBCC SHOW_STATISTICS('dbo.USERS', 'PK_USERS');


-- =============================================================================
-- 2) PARAMETER SNIFFING
-- =============================================================================
-- The optimizer compiles the plan for the FIRST parameter value it sees and
-- caches it. If that value is non-representative, later calls get a bad plan.
--
-- Symptoms:
--   * Same proc runs fast for one user, slow for another.
--   * sys.dm_exec_query_stats shows wildly variable elapsed time.
--
-- Mitigations (pick one):
--   a) OPTION (RECOMPILE) on the statement  -> compile fresh every time (CPU cost)
--   b) OPTION (OPTIMIZE FOR (@p = <literal>))-> pin to a known-good value
--   c) OPTION (OPTIMIZE FOR UNKNOWN)         -> use density average (legacy CE)
--   d) Refactor: split into branches by selectivity, or use local variables.
--   e) Enable the SQL 2017+ "adaptive joins" + "memory grant feedback" features.
--
-- Example template:

CREATE OR ALTER PROCEDURE dbo.GetUsersByCity_Sniff
    @City NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM   dbo.USERS
    WHERE  FirstName = @City   -- pretend this is City
    OPTION (RECOMPILE);
END


-- =============================================================================
-- 3) QUERY STORE  - "flight recorder" for query performance
-- =============================================================================
-- Enable per DB (already on by default on Azure SQL DB / SQL 2022).
-- ALTER DATABASE LEARNING SET QUERY_STORE = ON
--   (OPERATION_MODE = READ_WRITE,
--    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
--    DATA_FLUSH_INTERVAL_SECONDS = 900,
--    MAX_STORAGE_SIZE_MB = 1024);
--
-- Useful views:
--   sys.query_store_query, sys.query_store_plan, sys.query_store_runtime_stats
--
-- Forcing a known-good plan (regression fix without code changes):
--   EXEC sp_query_store_force_plan @query_id = ..., @plan_id = ...;


-- =============================================================================
-- 4) TABLE PARTITIONING  - large tables, sliding-window ETL
-- =============================================================================
-- Three objects work together:
--   a) PARTITION FUNCTION  - defines boundary values (e.g. monthly).
--   b) PARTITION SCHEME    - maps partitions to filegroups.
--   c) Table created ON the partition scheme using the partition column.
--
-- Sliding-window pattern: SWITCH old partition OUT to an archive table,
-- SPLIT a new empty boundary at the leading edge. Both are metadata-only
-- operations -> near-instant on huge tables.

IF EXISTS (SELECT 1 FROM sys.partition_schemes  WHERE name = 'PS_Sales_Monthly') DROP PARTITION SCHEME   PS_Sales_Monthly;
IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'PF_Sales_Monthly') DROP PARTITION FUNCTION PF_Sales_Monthly;

CREATE PARTITION FUNCTION PF_Sales_Monthly (DATE)
AS RANGE RIGHT FOR VALUES ('2026-01-01', '2026-02-01', '2026-03-01', '2026-04-01');

-- For demo we put all partitions on PRIMARY. Real-world: spread to filegroups.
CREATE PARTITION SCHEME PS_Sales_Monthly
AS PARTITION PF_Sales_Monthly ALL TO ([PRIMARY]);

IF OBJECT_ID('dbo.Sales','U') IS NOT NULL DROP TABLE dbo.Sales;
CREATE TABLE dbo.Sales
(
    SaleId    BIGINT IDENTITY(1,1) NOT NULL,
    SaleDate  DATE                 NOT NULL,
    Amount    DECIMAL(12,2)        NOT NULL,
    CONSTRAINT PK_Sales PRIMARY KEY CLUSTERED (SaleDate, SaleId)   -- partition col MUST be in clustered key
) ON PS_Sales_Monthly (SaleDate);

-- Add a leading boundary for the next month (split the rightmost empty partition):
ALTER PARTITION SCHEME   PS_Sales_Monthly NEXT USED [PRIMARY];
ALTER PARTITION FUNCTION PF_Sales_Monthly() SPLIT RANGE ('2026-05-01');

-- Inspect partitions and row counts:
SELECT  p.partition_number, p.rows, pf.boundary_value_on_right,
        prv.value AS boundary_value
FROM    sys.partitions p
JOIN    sys.indexes i           ON i.object_id = p.object_id AND i.index_id = p.index_id
JOIN    sys.partition_schemes ps ON ps.data_space_id = i.data_space_id
JOIN    sys.partition_functions pf ON pf.function_id = ps.function_id
LEFT JOIN sys.partition_range_values prv
    ON  prv.function_id = pf.function_id AND prv.boundary_id = p.partition_number
WHERE   p.object_id = OBJECT_ID('dbo.Sales')
ORDER  BY p.partition_number;


-- =============================================================================
-- 5) COLUMNSTORE INDEXES  - analytics / star schema workloads
-- =============================================================================
-- Stores data column-by-column with heavy compression + batch-mode execution.
-- Two flavors:
--   * CLUSTERED COLUMNSTORE     - the entire table is columnstore.
--                                 Great for fact tables, append-mostly loads.
--   * NONCLUSTERED COLUMNSTORE  - on top of a rowstore; OLTP + analytics on
--                                 the same table.
--
-- Sizing: rowgroups of up to ~1,048,576 rows compress together. Tiny inserts
-- form a "delta store" (rowstore) until the tuple mover migrates them.

IF OBJECT_ID('dbo.FactSales','U') IS NOT NULL DROP TABLE dbo.FactSales;
CREATE TABLE dbo.FactSales
(
    SaleId      BIGINT IDENTITY(1,1) NOT NULL,
    ProductId   INT    NOT NULL,
    CustomerId  INT    NOT NULL,
    SaleDate    DATE   NOT NULL,
    Quantity    INT    NOT NULL,
    Amount      DECIMAL(12,2) NOT NULL
);

CREATE CLUSTERED COLUMNSTORE INDEX CCI_FactSales ON dbo.FactSales;

-- Inspect compression / rowgroup health:
SELECT  OBJECT_NAME(object_id) AS table_name,
        row_group_id, state_desc, total_rows, deleted_rows, size_in_bytes
FROM    sys.dm_db_column_store_row_group_physical_stats
WHERE   object_id = OBJECT_ID('dbo.FactSales');


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * Stats drive cardinality estimates -> bad stats = bad plans.
   * Parameter sniffing: cached plan from first param; mitigate with RECOMPILE
     / OPTIMIZE FOR / refactor.
   * Query Store = flight recorder; force good plans to stop regressions.
   * Partitioning enables sliding-window ETL via metadata-only SWITCH / SPLIT.
   * Columnstore = compressed, batch-mode, analytics-friendly; fact tables.
   ============================================================================= */
