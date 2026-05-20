/* =============================================================================
   18_CDC_FullText_External_Tables.sql
   -----------------------------------------------------------------------------
   Specialized "data movement / discovery" features:
       1) Change Tracking (lightweight) vs Change Data Capture (rich history).
       2) Service Broker - reliable messaging inside SQL Server.
       3) Full-text search: CONTAINS / FREETEXT.
       4) Cross-server queries: OPENROWSET / OPENQUERY / linked servers.
       5) BULK INSERT / bcp - high-throughput data loading.
       6) External tables / PolyBase - federated query against ADLS/S3/Oracle.
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) CHANGE TRACKING vs CHANGE DATA CAPTURE (CDC)
-- =============================================================================
-- CHANGE TRACKING (CT):
--   * Tracks WHAT changed (PK + operation), not the old/new values.
--   * Lightweight, synchronous, low overhead.
--   * Use case: incremental sync (ETL "give me everything that changed since
--     version X").
--
-- CHANGE DATA CAPTURE (CDC):
--   * Reads the transaction log asynchronously into change tables.
--   * Captures OLD + NEW values + operation + LSN.
--   * Heavier; requires SQL Agent; great for audit / event sourcing / replicate
--     to a data warehouse.
--
-- Enabling (templates - require sysadmin):
-- ALTER DATABASE LEARNING SET CHANGE_TRACKING = ON
--   (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);
-- ALTER TABLE dbo.USERS ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON);
--
-- EXEC sys.sp_cdc_enable_db;
-- EXEC sys.sp_cdc_enable_table
--   @source_schema = N'dbo', @source_name = N'USERS', @role_name = NULL;
--
-- Query CT changes since version @last:
-- SELECT * FROM CHANGETABLE(CHANGES dbo.USERS, @last) AS ct;
--
-- Query CDC changes:
-- SELECT * FROM cdc.fn_cdc_get_all_changes_dbo_USERS(@from_lsn, @to_lsn, 'all');


-- =============================================================================
-- 2) SERVICE BROKER  -  reliable async messaging inside SQL Server
-- =============================================================================
-- Queues, dialogs, contracts, activation procs.
-- Use cases: decoupled processing inside the DB, durable work queues,
-- event-driven workflows without an external broker.
-- Often replaced today by external brokers (Service Bus / RabbitMQ / Kafka).
--
-- Enable on a DB: ALTER DATABASE LEARNING SET ENABLE_BROKER;
-- Core objects: CREATE MESSAGE TYPE / CONTRACT / QUEUE / SERVICE.
-- Send:    SEND ON CONVERSATION @dlg MESSAGE TYPE [...] (@msg);
-- Receive: WAITFOR (RECEIVE TOP (1) ... FROM dbo.MyQueue), TIMEOUT 1000;


-- =============================================================================
-- 3) FULL-TEXT SEARCH
-- =============================================================================
-- A separate index built by the Full-Text Engine for word-level search,
-- thesaurus, stemming, and fuzzy matching.
-- Prereq: Full-Text feature installed (sql_fulltext_service).
--
-- Setup template:
-- CREATE FULLTEXT CATALOG ft_catalog AS DEFAULT;
-- CREATE FULLTEXT INDEX ON dbo.Articles (Title LANGUAGE 1033, Body LANGUAGE 1033)
--     KEY INDEX PK_Articles ON ft_catalog
--     WITH STOPLIST = SYSTEM, CHANGE_TRACKING AUTO;
--
-- Querying:
--   CONTAINS  - boolean + proximity ("phrase", AND/OR/NEAR, prefix*, inflectional).
--   FREETEXT  - looser, meaning-based; stems and synonyms applied.
--
-- SELECT * FROM dbo.Articles WHERE CONTAINS(Body, 'NEAR((sql, performance), 5)');
-- SELECT * FROM dbo.Articles WHERE FREETEXT(Body, 'tuning indexes');
--
-- Alternative for modern workloads: vector search / semantic search (SQL 2025+,
-- Azure SQL DB). REGEXP_LIKE for pattern matching (SQL 2025).


-- =============================================================================
-- 4) CROSS-SERVER QUERIES
-- =============================================================================
-- LINKED SERVER (persistent registration):
-- EXEC sp_addlinkedserver @server = N'OTHERSRV', @srvproduct = N'', @provider = N'SQLNCLI';
-- SELECT * FROM OTHERSRV.SomeDb.dbo.SomeTable;       -- 4-part name
--
-- OPENQUERY - pass through to remote (executes remotely, often faster):
-- SELECT * FROM OPENQUERY(OTHERSRV, 'SELECT TOP 100 * FROM dbo.SomeTable');
--
-- OPENROWSET - one-off, no linked server needed:
-- SELECT * FROM OPENROWSET(
--     'SQLNCLI',
--     'Server=OTHERSRV;Trusted_Connection=yes;',
--     'SELECT * FROM SomeDb.dbo.SomeTable');
--
-- OPENROWSET(BULK ...) - load files directly:
-- SELECT * FROM OPENROWSET(
--     BULK 'C:\data\users.json', SINGLE_CLOB) AS j
-- CROSS APPLY OPENJSON(BulkColumn);


-- =============================================================================
-- 5) BULK INSERT / bcp
-- =============================================================================
-- High-speed loader. Use minimal logging (recovery model SIMPLE / BULK_LOGGED,
-- TABLOCK hint) and a covering staging table.
--
-- BULK INSERT dbo.StagingUsers
--   FROM 'C:\data\users.csv'
--   WITH (FIRSTROW = 2, FIELDTERMINATOR = ',', ROWTERMINATOR = '0x0A',
--         TABLOCK, MAXERRORS = 10, ERRORFILE = 'C:\data\users_err.log');
--
-- Command-line bcp.exe is preferred for huge files - it can run native
-- format and parallelize.


-- =============================================================================
-- 6) EXTERNAL TABLES / PolyBase
-- =============================================================================
-- Federated query: T-SQL over data that lives outside SQL Server -
-- Azure Data Lake, S3, Oracle, MongoDB, generic ODBC, parquet / CSV files.
--
-- High-level steps:
--   1. CREATE MASTER KEY (once).
--   2. CREATE DATABASE SCOPED CREDENTIAL for the remote auth.
--   3. CREATE EXTERNAL DATA SOURCE (URL + credential).
--   4. CREATE EXTERNAL FILE FORMAT (CSV / Parquet).
--   5. CREATE EXTERNAL TABLE matching the remote schema.
--
-- After that you SELECT from it like any other table - SQL pushes filters
-- down to the remote engine when possible.
--
-- Modern alternative: OPENROWSET(BULK ...) directly against Parquet on Azure
-- Storage (Azure SQL DB / Synapse Serverless) - no external table required.


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * Change Tracking = "what PKs changed?"; CDC = "give me old/new values + LSN".
   * Full-text:  CONTAINS = boolean/proximity, FREETEXT = meaning-based.
   * OPENQUERY pushes the query to the remote server - faster than 4-part name.
   * BULK INSERT with TABLOCK + SIMPLE/BULK_LOGGED = minimal logging.
   * External tables / PolyBase = T-SQL over remote / file data; great for lakehouse.
   ============================================================================= */
