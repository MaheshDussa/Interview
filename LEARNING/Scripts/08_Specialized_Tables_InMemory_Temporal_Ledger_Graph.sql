/* =============================================================================
   Script8.sql  -  SPECIALIZED TABLE TYPES in SQL Server
   -----------------------------------------------------------------------------
   SQL Server has several "specialized" table flavors beyond the regular
   disk-based rowstore. Each solves a specific problem. This script covers
   the four most-asked-about in interviews:

       1) MEMORY-OPTIMIZED tables (Hekaton / In-Memory OLTP)
       2) TEMPORAL  (system-versioned) tables - automatic history tracking
       3) LEDGER    tables                    - tamper-evident, cryptographically verifiable
       4) GRAPH     tables (NODE / EDGE)      - native graph storage + MATCH queries

   When to use which (quick guide):
     * Memory-optimized -> ultra-high throughput OLTP, hot lookup tables,
                           session state, queues. Eliminates locks/latches.
     * Temporal         -> "as of" queries, audit, regulatory history, slowly
                           changing dimensions without writing triggers.
     * Ledger           -> compliance / anti-tamper requirements (finance,
                           supply chain, HR records). Proof that data was
                           not modified outside the documented history.
     * Graph            -> many-to-many relationships with traversal queries
                           (org charts, social networks, fraud rings).
   ============================================================================= */


-- =============================================================================
-- 1) MEMORY-OPTIMIZED TABLES (In-Memory OLTP, code-named "Hekaton")
-- =============================================================================
-- Plain English:
--   The whole table lives in RAM. It still survives restarts because changes
--   are written to a special filegroup on disk in the background. There are
--   NO locks and NO latches - concurrency is handled with optimistic
--   multi-versioning, which is why it scales so well under heavy OLTP load.
--
-- Prerequisites:
--   * The database must have a MEMORY_OPTIMIZED_DATA filegroup.
--   * Best paired with NATIVELY COMPILED stored procedures for max perf.
--
-- Two durability modes:
--   * SCHEMA_AND_DATA : durable, data survives restart (default).
--   * SCHEMA_ONLY     : data is wiped on restart - great for session state,
--                       staging tables, caches.
--
-- Interview talking points:
--   * Uses HASH or NONCLUSTERED indexes - NO clustered index, NO heaps.
--   * Hash index needs BUCKET_COUNT sized to ~ expected distinct keys.
--   * No FOREIGN KEYs to disk-based tables (only to other memory tables).
--   * Cannot use TRUNCATE, MERGE, or some other DDL.
-- -----------------------------------------------------------------------------

-- One-time DB prep (commented; uncomment only if the filegroup doesn't exist):
-- ALTER DATABASE LEARNING ADD FILEGROUP MemFG CONTAINS MEMORY_OPTIMIZED_DATA;
-- ALTER DATABASE LEARNING ADD FILE
--     (NAME = 'MemData', FILENAME = 'C:\SQLData\LEARNING_MemData') TO FILEGROUP MemFG;

IF OBJECT_ID('dbo.SessionCache', 'U') IS NOT NULL DROP TABLE dbo.SessionCache;

CREATE TABLE dbo.SessionCache
(
    SessionId   UNIQUEIDENTIFIER NOT NULL,
    UserId      INT              NOT NULL,
    LastSeenUtc DATETIME2        NOT NULL,
    Payload     NVARCHAR(4000)   NULL,

    -- HASH index: fast equality lookup. BUCKET_COUNT should be ~1-2x distinct keys.
    CONSTRAINT PK_SessionCache PRIMARY KEY NONCLUSTERED HASH (SessionId) WITH (BUCKET_COUNT = 100000),

    -- Range / ordered lookups need a regular NONCLUSTERED index (B-tree).
    INDEX IX_SessionCache_UserId NONCLUSTERED (UserId)
)
WITH (MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_AND_DATA);
GO

-- Natively compiled stored procedure for the hot path.
-- These compile down to C / machine code on first use -> ~5-30x faster than interpreted T-SQL.
CREATE OR ALTER PROCEDURE dbo.UpsertSession
    @SessionId UNIQUEIDENTIFIER,
    @UserId    INT,
    @Payload   NVARCHAR(4000)
WITH NATIVE_COMPILATION, SCHEMABINDING
AS
BEGIN ATOMIC WITH
(
    TRANSACTION ISOLATION LEVEL = SNAPSHOT,
    LANGUAGE = N'us_english'
)
    UPDATE dbo.SessionCache
       SET UserId = @UserId, Payload = @Payload, LastSeenUtc = SYSUTCDATETIME()
     WHERE SessionId = @SessionId;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.SessionCache (SessionId, UserId, LastSeenUtc, Payload)
        VALUES (@SessionId, @UserId, SYSUTCDATETIME(), @Payload);
END
GO


-- =============================================================================
-- 2) TEMPORAL (SYSTEM-VERSIONED) TABLES  - automatic history
-- =============================================================================
-- Plain English:
--   You declare two hidden datetime2 columns and a "history" table. Every
--   UPDATE / DELETE on the main table is automatically copied to history
--   with its valid time range. You can then query the data AS IT WAS at any
--   point in the past - no triggers, no audit code.
--
-- Special clauses:
--   * GENERATED ALWAYS AS ROW START / END    -> the system manages these
--   * PERIOD FOR SYSTEM_TIME (StartCol, EndCol)
--   * WITH (SYSTEM_VERSIONING = ON ( HISTORY_TABLE = ... ))
--
-- Time-travel queries (FOR SYSTEM_TIME):
--   * AS OF <datetime>
--   * BETWEEN <from> AND <to>
--   * FROM <from> TO <to>
--   * CONTAINED IN (<from>, <to>)
--   * ALL                                    -> current + every history row
--
-- Interview talking points:
--   * Times are UTC and inclusive-start / exclusive-end.
--   * History table is read-only via T-SQL (insertable only by the engine).
--   * Cannot TRUNCATE while versioning is on.
--   * Great pairing with Row-Level Security for "who saw what when" audit.
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Employee', 'U') IS NOT NULL
BEGIN
    -- Turn versioning off so we can drop both tables cleanly.
    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Employee' AND temporal_type = 2)
        ALTER TABLE dbo.Employee SET (SYSTEM_VERSIONING = OFF);
    DROP TABLE IF EXISTS dbo.Employee;
    DROP TABLE IF EXISTS dbo.Employee_History;
END

CREATE TABLE dbo.Employee
(
    EmpId      INT           NOT NULL PRIMARY KEY,
    EmpName    NVARCHAR(100) NOT NULL,
    Department NVARCHAR(50)  NOT NULL,
    Salary     DECIMAL(10,2) NOT NULL,

    -- The two period columns. HIDDEN keeps them out of SELECT * results.
    ValidFrom  DATETIME2 GENERATED ALWAYS AS ROW START   HIDDEN NOT NULL,
    ValidTo    DATETIME2 GENERATED ALWAYS AS ROW END     HIDDEN NOT NULL,
    PERIOD FOR SYSTEM_TIME (ValidFrom, ValidTo)
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.Employee_History));
GO

-- Try it out: any DML now silently writes history.
INSERT INTO dbo.Employee (EmpId, EmpName, Department, Salary) VALUES
(1, 'Alice', 'Sales', 60000),
(2, 'Bob',   'IT',    80000);

UPDATE dbo.Employee SET Salary = 65000 WHERE EmpId = 1;   -- previous row copied to history
DELETE FROM dbo.Employee WHERE EmpId = 2;                 -- final state copied to history

-- Time-travel queries:
SELECT * FROM dbo.Employee FOR SYSTEM_TIME AS OF '2026-05-18T00:00:00';   -- snapshot at a point in time
SELECT * FROM dbo.Employee FOR SYSTEM_TIME ALL;                            -- current + every version
SELECT * FROM dbo.Employee_History;                                        -- raw history table


-- =============================================================================
-- 3) LEDGER TABLES  - tamper-evident, cryptographically verifiable
-- =============================================================================
-- Plain English:
--   Like temporal tables, but every block of transactions is hashed and the
--   hashes are chained (Merkle tree). The database periodically writes a
--   "digest" that can be stored OUTSIDE the database (e.g. Azure Blob with
--   immutability policy). Any later tampering breaks the hash chain and is
--   detected by a verification procedure.
--
-- Two flavors:
--   * APPEND_ONLY ledger : INSERT only - no UPDATE/DELETE allowed.
--     Perfect for event logs, audit trails.
--   * UPDATABLE  ledger  : full CRUD, but the engine maintains a separate
--     history + ledger view to make changes provable.
--
-- Built-in columns/views added by the engine:
--   * ledger_start_transaction_id / ledger_end_transaction_id
--   * ledger_start_sequence_number / ledger_end_sequence_number
--   * <table>_Ledger view              -> chronological change log
--   * sys.database_ledger_transactions -> per-transaction metadata + hash
--
-- Verification:
--   EXEC sys.sp_verify_database_ledger ...   (compares stored digests to live data)
--
-- Interview talking points:
--   * "Tamper-evident, not tamper-proof" - a DBA with sysadmin can still try
--     to alter rows, but verification will fail and prove it.
--   * Best paired with digests written to immutable storage outside SQL.
--   * Requires Enterprise edition (or Azure SQL DB).
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.PaymentLedger', 'U') IS NOT NULL DROP TABLE dbo.PaymentLedger;

-- APPEND_ONLY ledger - financial audit trail use case.
CREATE TABLE dbo.PaymentLedger
(
    PaymentId   INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AccountId   INT               NOT NULL,
    Amount      DECIMAL(18,2)     NOT NULL,
    PostedUtc   DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME()
)
WITH (LEDGER = ON (APPEND_ONLY = ON));
GO

INSERT INTO dbo.PaymentLedger (AccountId, Amount) VALUES
(1001,  250.00),
(1002, 1000.00);

-- UPDATE / DELETE on an append-only ledger are blocked by the engine - try it:
-- UPDATE dbo.PaymentLedger SET Amount = 0 WHERE PaymentId = 1;   -- ERROR

-- Inspect the engine-generated ledger view + transaction metadata:
SELECT * FROM dbo.PaymentLedger_Ledger;                 -- one row per change
SELECT TOP 10 * FROM sys.database_ledger_transactions   -- hashed transaction log
ORDER BY commit_time DESC;


-- =============================================================================
-- 4) GRAPH TABLES  - NODE and EDGE tables + MATCH queries
-- =============================================================================
-- Plain English:
--   * NODE table = entity (Person, Product, Account).
--   * EDGE table = relationship between two nodes (FollowedBy, BoughtFrom).
--   * Engine auto-adds $node_id / $from_id / $to_id hidden columns.
--   * The MATCH clause uses arrow notation to express traversal:
--         (a)-[friend]->(b)
--   * Recursive / shortest-path is available via SHORTEST_PATH.
--
-- When to prefer graph over plain FK / junction tables:
--   * Variable-depth traversals (friends-of-friends, supply chains, fraud rings).
--   * Pattern matching that would otherwise need many self-joins.
--
-- Interview talking points:
--   * It is still relational storage underneath - performance still depends
--     on indexes on the edge columns.
--   * Constraints on edges (EDGE CONSTRAINT) limit which node types can
--     connect to which - the graph equivalent of a foreign key.
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.Follows', 'U') IS NOT NULL DROP TABLE dbo.Follows;
IF OBJECT_ID('dbo.Person',  'U') IS NOT NULL DROP TABLE dbo.Person;

CREATE TABLE dbo.Person
(
    PersonId INT          NOT NULL PRIMARY KEY,
    Name     NVARCHAR(50) NOT NULL
) AS NODE;                                                      -- <-- NODE table

CREATE TABLE dbo.Follows
(
    SinceUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    -- Constrains which node tables can be on each side of the edge.
    CONSTRAINT EC_Follows CONNECTION (dbo.Person TO dbo.Person)
) AS EDGE;                                                      -- <-- EDGE table
GO

-- Seed nodes.
INSERT INTO dbo.Person (PersonId, Name) VALUES
(1, 'Alice'),
(2, 'Bob'),
(3, 'Carol'),
(4, 'David');

-- Seed edges using $node_id of each side.
INSERT INTO dbo.Follows ($from_id, $to_id)
SELECT a.$node_id, b.$node_id
FROM dbo.Person a, dbo.Person b
WHERE (a.Name = 'Alice' AND b.Name = 'Bob')
   OR (a.Name = 'Bob'   AND b.Name = 'Carol')
   OR (a.Name = 'Carol' AND b.Name = 'David')
   OR (a.Name = 'Alice' AND b.Name = 'Carol');

-- 1-hop query: "Who does Alice follow?"
SELECT followee.Name
FROM dbo.Person  AS follower,
     dbo.Follows AS f,
     dbo.Person  AS followee
WHERE MATCH(follower-(f)->followee)
  AND follower.Name = 'Alice';

-- 2-hop query: "Friends of friends of Alice" (excluding direct follows).
SELECT DISTINCT fof.Name
FROM dbo.Person  AS me,
     dbo.Follows AS f1,
     dbo.Person  AS friend,
     dbo.Follows AS f2,
     dbo.Person  AS fof
WHERE MATCH(me-(f1)->friend-(f2)->fof)
  AND me.Name = 'Alice'
  AND fof.Name <> 'Alice';

-- Shortest path (SQL Server 2019+). Uses SHORTEST_PATH and FOR PATH aliases.
SELECT  start.Name                                  AS StartPerson,
        LAST_VALUE(reached.Name) WITHIN GROUP (GRAPH PATH) AS EndPerson,
        STRING_AGG(reached.Name, ' -> ') WITHIN GROUP (GRAPH PATH) AS Path
FROM    dbo.Person  AS start,
        dbo.Follows FOR PATH AS f,
        dbo.Person  FOR PATH AS reached
WHERE   MATCH(SHORTEST_PATH(start(-(f)->reached)+))
    AND start.Name = 'Alice';


/* =============================================================================
   QUICK INTERVIEW REVIEW
   -----------------------------------------------------------------------------
   * Memory-Optimized -> latch-free OLTP; hash indexes need BUCKET_COUNT;
     pair with natively compiled procs; SCHEMA_ONLY for ephemeral data.
   * Temporal         -> two GENERATED ALWAYS period columns + history table;
     query with FOR SYSTEM_TIME AS OF / ALL / BETWEEN.
   * Ledger           -> append-only or updatable; hashes chained + digests
     stored externally; verified with sys.sp_verify_database_ledger.
   * Graph            -> NODE + EDGE tables, $node_id / $from_id / $to_id,
     MATCH(a-(e)->b) for traversal, SHORTEST_PATH for variable depth.
   ============================================================================= */
