/* =============================================================================
   17_Admin_Backups_DMVs_Monitoring.sql
   -----------------------------------------------------------------------------
   The "DBA hat" questions every senior dev should be able to answer:
       1) Recovery models + backup/restore strategy.
       2) DBCC CHECKDB and corruption handling.
       3) Index + statistics maintenance.
       4) DMVs every developer should know.
       5) Wait stats - "what is SQL waiting on?".
       6) SQL Server Agent jobs.
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) RECOVERY MODELS + BACKUP TYPES
-- =============================================================================
-- Recovery models:
--   SIMPLE        - log is auto-truncated on checkpoint. No log backups,
--                   point-in-time recovery NOT possible. Good for dev / DWs.
--   FULL          - log retained until backed up. Point-in-time recovery.
--                   Required for AlwaysOn AGs and log shipping.
--   BULK_LOGGED   - like FULL but bulk ops are minimally logged. Sacrifices
--                   PITR within a bulk-logged interval. Niche.
--
-- Backup types:
--   FULL          - everything.
--   DIFFERENTIAL  - changes since last FULL.
--   LOG           - log records since last LOG / FULL.
--   COPY_ONLY     - does not affect the backup chain (ad-hoc snapshots).
--
-- Typical FULL recovery rotation: weekly FULL + daily DIFF + log backups
-- every 15 min. Restore order: FULL -> latest DIFF -> all LOGs since DIFF.

-- Inspect current recovery model:
SELECT name, recovery_model_desc, log_reuse_wait_desc, state_desc
FROM   sys.databases
WHERE  database_id > 4;

-- Find recent backups for THIS db:
SELECT TOP (10) database_name, type, backup_start_date, backup_finish_date,
       CAST(backup_size / 1024.0 / 1024.0 AS DECIMAL(10,2)) AS size_mb
FROM   msdb.dbo.backupset
WHERE  database_name = DB_NAME()
ORDER  BY backup_finish_date DESC;

-- Templates (commented - don't auto-run in tutorial):
-- BACKUP DATABASE LEARNING TO DISK = 'C:\Backups\LEARNING_FULL.bak' WITH COMPRESSION, INIT;
-- BACKUP DATABASE LEARNING TO DISK = 'C:\Backups\LEARNING_DIFF.bak' WITH DIFFERENTIAL, COMPRESSION;
-- BACKUP LOG      LEARNING TO DISK = 'C:\Backups\LEARNING_LOG.trn'  WITH COMPRESSION;
--
-- Point-in-time restore:
-- RESTORE DATABASE LEARNING FROM DISK = 'FULL.bak' WITH NORECOVERY, REPLACE;
-- RESTORE DATABASE LEARNING FROM DISK = 'DIFF.bak' WITH NORECOVERY;
-- RESTORE LOG      LEARNING FROM DISK = 'LOG.trn'  WITH STOPAT = '2026-01-15 14:30';
-- RESTORE DATABASE LEARNING WITH RECOVERY;


-- =============================================================================
-- 2) DBCC CHECKDB  - corruption detection
-- =============================================================================
-- Runs structural + logical consistency checks on every page.
-- Schedule weekly on production. If it returns errors:
--   1. RESTORE from a known-good backup (preferred).
--   2. Only use REPAIR_ALLOW_DATA_LOSS as absolute last resort.
--
-- DBCC CHECKDB ('LEARNING') WITH NO_INFOMSGS, ALL_ERRORMSGS;


-- =============================================================================
-- 3) INDEX + STATISTICS MAINTENANCE
-- =============================================================================
-- Fragmentation guidelines (rules of thumb):
--   < 5%   : leave it alone
--   5-30%  : ALTER INDEX ... REORGANIZE (online, light)
--   > 30%  : ALTER INDEX ... REBUILD    (heavier; can be ONLINE = ON)
--
-- Modern alternative: use community scripts (Ola Hallengren / IndexOptimize)
-- which handle thresholds, columnstores, partitioned tables.

SELECT  OBJECT_NAME(ips.object_id) AS table_name,
        i.name AS index_name,
        ips.avg_fragmentation_in_percent,
        ips.page_count
FROM    sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN    sys.indexes i ON i.object_id = ips.object_id AND i.index_id = ips.index_id
WHERE   ips.page_count > 100
ORDER  BY ips.avg_fragmentation_in_percent DESC;


-- =============================================================================
-- 4) DMVs every developer should know
-- =============================================================================
-- Currently running requests (live activity):
SELECT  r.session_id, r.status, r.command, r.wait_type, r.wait_time,
        r.cpu_time, r.total_elapsed_time, r.blocking_session_id,
        SUBSTRING(t.text, r.statement_start_offset/2 + 1,
                  (CASE r.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
                   ELSE r.statement_end_offset END - r.statement_start_offset)/2 + 1) AS sql_text
FROM    sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE   r.session_id <> @@SPID;

-- Top CPU-consuming queries (from plan cache):
SELECT TOP (10)
        qs.execution_count,
        qs.total_worker_time / 1000 AS total_cpu_ms,
        qs.total_worker_time / qs.execution_count / 1000 AS avg_cpu_ms,
        qs.total_logical_reads, qs.last_execution_time,
        SUBSTRING(t.text, qs.statement_start_offset/2 + 1,
                  (CASE qs.statement_end_offset WHEN -1 THEN DATALENGTH(t.text)
                   ELSE qs.statement_end_offset END - qs.statement_start_offset)/2 + 1) AS sql_text
FROM    sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t
ORDER  BY qs.total_worker_time DESC;

-- Connections + sessions:
SELECT session_id, login_name, host_name, program_name, status, cpu_time, memory_usage
FROM   sys.dm_exec_sessions
WHERE  is_user_process = 1;


-- =============================================================================
-- 5) WAIT STATS  - "what is SQL waiting on?"
-- =============================================================================
-- Top wait types since last restart (filter out benign system waits):
SELECT TOP (15)
        wait_type,
        waiting_tasks_count,
        wait_time_ms,
        wait_time_ms / NULLIF(waiting_tasks_count, 0) AS avg_wait_ms,
        signal_wait_time_ms
FROM    sys.dm_os_wait_stats
WHERE   wait_type NOT IN (
            'CLR_SEMAPHORE','LAZYWRITER_SLEEP','RESOURCE_QUEUE','SLEEP_TASK',
            'SLEEP_SYSTEMTASK','SQLTRACE_BUFFER_FLUSH','WAITFOR','LOGMGR_QUEUE',
            'CHECKPOINT_QUEUE','REQUEST_FOR_DEADLOCK_SEARCH','XE_TIMER_EVENT',
            'BROKER_TASK_STOP','CLR_MANUAL_EVENT','CLR_AUTO_EVENT',
            'DISPATCHER_QUEUE_SEMAPHORE','TRACEWRITE','XE_DISPATCHER_WAIT',
            'XE_DISPATCHER_JOIN','BROKER_TRANSMITTER','BROKER_EVENTHANDLER',
            'BROKER_RECEIVE_WAITFOR','ONDEMAND_TASK_QUEUE')
   AND  waiting_tasks_count > 0
ORDER  BY wait_time_ms DESC;

-- Common waits to recognize:
--   PAGEIOLATCH_*    -> reads from disk (slow IO or missing indexes)
--   CXPACKET / CXCONSUMER -> parallelism (often benign; check skew)
--   LCK_M_*          -> blocking / locking contention
--   WRITELOG         -> transaction log throughput bottleneck
--   ASYNC_NETWORK_IO -> client not consuming results fast enough
--   SOS_SCHEDULER_YIELD -> CPU pressure


-- =============================================================================
-- 6) SQL SERVER AGENT JOBS
-- =============================================================================
-- Agent jobs run scheduled T-SQL / SSIS / PowerShell. Common uses:
--   * Nightly backups.
--   * Index + stats maintenance.
--   * DBCC CHECKDB.
--   * ETL.
-- Inspect last 50 job runs:
-- SELECT TOP (50) j.name, h.run_date, h.run_time, h.run_duration,
--                 h.run_status, h.message
-- FROM   msdb.dbo.sysjobs j
-- JOIN   msdb.dbo.sysjobhistory h ON j.job_id = h.job_id
-- ORDER  BY h.run_date DESC, h.run_time DESC;


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * FULL recovery + log backups = point-in-time recovery.
   * Schedule DBCC CHECKDB weekly; restore beats REPAIR_ALLOW_DATA_LOSS.
   * Reorg < 30% frag, Rebuild > 30%.
   * sys.dm_exec_requests / _query_stats / _sessions = your live-debug toolkit.
   * Always include wait-stats analysis when troubleshooting performance.
   ============================================================================= */
