/* =============================================================================
   11_Transactions_And_Isolation_Levels.sql
   -----------------------------------------------------------------------------
   ACID  =  Atomicity, Consistency, Isolation, Durability.
   A TRANSACTION is a logical unit of work that is all-or-nothing.

   Skeleton:
       SET XACT_ABORT ON;          -- any runtime error => auto ROLLBACK
       BEGIN TRY
           BEGIN TRAN;
              -- DML here
           COMMIT TRAN;
       END TRY
       BEGIN CATCH
           IF XACT_STATE() <> 0 ROLLBACK TRAN;
           THROW;
       END CATCH;

   Read phenomena (the 4 things isolation levels prevent or allow):
       Dirty read         : seeing UNCOMMITTED changes from another tx.
       Non-repeatable read: same row, different values when re-read.
       Phantom read       : same range query, NEW rows appear on re-read.
       Lost update        : two tx read+modify+write the same row, one wins.

   Isolation levels (lowest to highest), and what each PREVENTS:
       READ UNCOMMITTED     none (dirty allowed)
       READ COMMITTED       dirty                                       (DEFAULT)
       READ COMMITTED SNAPSHOT (RCSI) - same guarantees as RC but uses row versions instead of S-locks
       REPEATABLE READ      dirty + non-repeatable
       SNAPSHOT             dirty + non-repeatable + phantom (versioned)
       SERIALIZABLE         all four; uses range locks (strictest, most blocking)

   Interview talking points:
     * NOLOCK = READ UNCOMMITTED hint. Fast but can read dirty/missing/duplicate rows.
     * Enable RCSI to remove most reader-writer blocking without code changes:
           ALTER DATABASE LEARNING SET READ_COMMITTED_SNAPSHOT ON;
     * SNAPSHOT requires ALLOW_SNAPSHOT_ISOLATION ON; tx sees the DB as of its start.
     * Deadlock = two tx blocking each other; victim is rolled back by SQL Server.
       Mitigate with consistent access order, smaller tx, proper indexes, UPDLOCK.
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) Basic transaction with TRY/CATCH/THROW
-- =============================================================================
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

        UPDATE dbo.USERS SET IsActive = 1 WHERE UserId = 1;
        UPDATE dbo.USERS SET IsActive = 1 WHERE UserId = 2;

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRAN;   -- only if a tx is still open
    THROW;                                -- re-raise with full error info
END CATCH;
GO


-- =============================================================================
-- 2) SAVEPOINTS  - partial rollback inside one tx
-- =============================================================================
BEGIN TRAN;

    UPDATE dbo.USERS SET IsActive = 0 WHERE UserId = 1;
    SAVE TRAN BeforeRiskyStep;

    UPDATE dbo.USERS SET FirstName = N'OOPS' WHERE UserId = 1;
    -- Decide we want to undo only the risky part:
    ROLLBACK TRAN BeforeRiskyStep;

COMMIT TRAN;
GO


-- =============================================================================
-- 3) NESTED transactions (SQL Server "pseudo-nested")
-- =============================================================================
-- Only the OUTERMOST COMMIT actually commits. Any ROLLBACK rolls back EVERYTHING.
-- @@TRANCOUNT increments per BEGIN TRAN and decrements per COMMIT.
PRINT @@TRANCOUNT;     -- 0
BEGIN TRAN;
    PRINT @@TRANCOUNT; -- 1
    BEGIN TRAN;
        PRINT @@TRANCOUNT; -- 2
    COMMIT TRAN;
    PRINT @@TRANCOUNT; -- 1
COMMIT TRAN;
PRINT @@TRANCOUNT;     -- 0
GO


-- =============================================================================
-- 4) Setting the isolation level for the SESSION
-- =============================================================================
-- Choose ONE - last one wins for the session.
-- SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
-- SET TRANSACTION ISOLATION LEVEL READ COMMITTED;     -- default
-- SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
-- SET TRANSACTION ISOLATION LEVEL SNAPSHOT;
-- SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;

-- Per-statement hints (override the session level):
SELECT * FROM dbo.USERS WITH (NOLOCK);          -- = READ UNCOMMITTED
SELECT * FROM dbo.USERS WITH (READPAST);        -- skip locked rows (queue patterns)
SELECT * FROM dbo.USERS WITH (UPDLOCK, HOLDLOCK);-- acquire and hold update locks (anti-deadlock pattern)


-- =============================================================================
-- 5) Detecting and resolving DEADLOCKS
-- =============================================================================
-- Two-session anti-pattern (run each block in a separate session):
--   Session A: BEGIN TRAN; UPDATE A; ... UPDATE B;
--   Session B: BEGIN TRAN; UPDATE B; ... UPDATE A;
-- SQL Server picks a victim (lowest cost / deadlock priority) and aborts it with
-- error 1205. Catch it and retry.

BEGIN TRY
    BEGIN TRAN;
        UPDATE dbo.USERS SET FirstName = N'A' WHERE UserId = 1;
        -- ... long running step ...
        UPDATE dbo.USERS SET FirstName = N'B' WHERE UserId = 2;
    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 1205
    BEGIN
        IF XACT_STATE() <> 0 ROLLBACK;
        -- Retry policy: small backoff + retry counter (omitted for brevity)
        PRINT 'Deadlock victim - retry suggested';
    END
    ELSE
    BEGIN
        IF XACT_STATE() <> 0 ROLLBACK;
        THROW;
    END
END CATCH;
GO


-- =============================================================================
-- 6) Inspecting locks / blocking
-- =============================================================================
-- Current locks held:
SELECT  resource_type, resource_database_id, resource_associated_entity_id,
        request_mode, request_status, request_session_id
FROM    sys.dm_tran_locks
WHERE   resource_database_id = DB_ID()
ORDER  BY request_session_id;

-- Who is blocking whom:
SELECT  blocking_session_id AS Blocker,
        session_id          AS BlockedSession,
        wait_type, wait_time, wait_resource, command
FROM    sys.dm_exec_requests
WHERE   blocking_session_id <> 0;


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * ACID + the 4 read phenomena.
   * READ COMMITTED is default; RCSI (row versioned) removes most blocking.
   * SNAPSHOT = "as of tx start"; SERIALIZABLE locks ranges (slowest).
   * NOLOCK == READ UNCOMMITTED - allows dirty/missing/duplicate rows.
   * @@TRANCOUNT, SAVEPOINTS, deadlock victim = error 1205 => retry.
   ============================================================================= */
