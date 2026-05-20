/* =============================================================================
   20_System_Procs_System_Tables_Shortcuts.sql
   -----------------------------------------------------------------------------
   A field guide to SQL Server's built-in metadata + the keyboard shortcuts
   that make you fast in SSMS / Azure Data Studio / VS Code (mssql ext).

   Sections:
       1) System stored procedures (sp_*)
       2) System catalog views (sys.*)
       3) INFORMATION_SCHEMA views (ANSI standard)
       4) Dynamic Management Views / Functions (sys.dm_*)
       5) Built-in metadata FUNCTIONS (OBJECT_ID, DB_NAME, ...)
       6) DBCC commands you actually use
       7) SSMS / Azure Data Studio / VS Code keyboard shortcuts
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) SYSTEM STORED PROCEDURES  (sp_*)
-- =============================================================================
-- Live in the [master] DB but are callable from any DB (the "sp_" prefix
-- triggers special name resolution). Cover almost every admin/inspection task.

-- ---- Object inspection ----------------------------------------------------
EXEC sp_help        'dbo.USERS';        -- columns, indexes, constraints, FKs
EXEC sp_helptext    'dbo.AssignRoleToUser';  -- source code of proc/view/UDF/trigger
EXEC sp_columns     @table_name = 'USERS';
EXEC sp_pkeys       @table_name = 'USERS';
EXEC sp_fkeys       @pktable_name = 'USERS';
EXEC sp_helpindex   'dbo.USERS';        -- all indexes on a table
EXEC sp_helpconstraint 'dbo.USERS';     -- all constraints
EXEC sp_depends     'dbo.USERS';        -- what depends on this object (legacy; prefer sys.dm_sql_referencing_entities)
EXEC sp_spaceused   'dbo.USERS';        -- row count + size on disk

-- ---- Database / instance metadata ----------------------------------------
EXEC sp_helpdb;                         -- list all DBs + files + sizes
EXEC sp_helpdb 'LEARNING';
EXEC sp_databases;                      -- ANSI-style list
EXEC sp_who2;                           -- active sessions + blocking (legacy but loved)
EXEC sp_lock;                           -- current locks (legacy; prefer sys.dm_tran_locks)
EXEC sp_helpserver;
EXEC sp_helplogins;                     -- server logins
EXEC sp_helpuser;                       -- DB users
EXEC sp_helprolemember 'db_owner';

-- ---- Code execution & dynamic SQL ----------------------------------------
-- sp_executesql N'SELECT ...', N'@p int', @p = 5;     -- parameterized dynamic SQL (preferred)

-- ---- Admin / DDL helpers --------------------------------------------------
-- EXEC sp_rename 'dbo.OldName',  'NewName';            -- rename table/proc/view
-- EXEC sp_rename 'dbo.USERS.OldCol', 'NewCol', 'COLUMN';
-- EXEC sp_recompile 'dbo.AssignRoleToUser';            -- force next call to recompile
-- EXEC sp_updatestats;                                 -- refresh all out-of-date stats
-- EXEC sp_configure 'show advanced options', 1; RECONFIGURE;
-- EXEC sp_configure;                                   -- list instance settings
-- EXEC sp_msforeachtable 'PRINT ''?''';                -- undocumented loop helper

-- ---- Procs you'll see in interviews --------------------------------------
--   sp_executesql       parameterized dynamic SQL
--   sp_help / sp_helptext / sp_helpindex / sp_helpconstraint
--   sp_who2 / sp_lock                       live activity
--   sp_rename / sp_recompile                DDL helpers
--   sp_configure                            instance options
--   sp_addlinkedserver / sp_addlinkedsrvlogin   linked servers
--   sp_send_dbmail                          email from SQL (Database Mail)
--   sp_start_job / sp_stop_job              SQL Agent jobs
--   sp_estimate_data_compression_savings    compression sizing


-- =============================================================================
-- 2) SYSTEM CATALOG VIEWS  (sys.*)
-- =============================================================================
-- The MODERN, supported way to query metadata. Prefer these over the old
-- sysobjects / syscolumns compatibility views.

-- All tables + schemas:
SELECT s.name AS schema_name, t.name AS table_name, t.create_date, t.modify_date
FROM   sys.tables t
JOIN   sys.schemas s ON s.schema_id = t.schema_id
ORDER  BY s.name, t.name;

-- Columns of a table:
SELECT c.column_id, c.name, ty.name AS data_type,
       c.max_length, c.precision, c.scale, c.is_nullable, c.is_identity
FROM   sys.columns c
JOIN   sys.types   ty ON ty.user_type_id = c.user_type_id
WHERE  c.object_id = OBJECT_ID('dbo.USERS')
ORDER  BY c.column_id;

-- Indexes + included columns:
SELECT i.name AS index_name, i.type_desc, i.is_unique, i.is_primary_key,
       c.name AS column_name, ic.is_included_column, ic.key_ordinal
FROM   sys.indexes i
JOIN   sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN   sys.columns       c  ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
WHERE  i.object_id = OBJECT_ID('dbo.USERS')
ORDER  BY i.index_id, ic.key_ordinal;

-- Foreign keys:
SELECT  fk.name AS fk_name,
        OBJECT_NAME(fk.parent_object_id)    AS child_table,
        cp.name                             AS child_col,
        OBJECT_NAME(fk.referenced_object_id) AS parent_table,
        cr.name                             AS parent_col,
        fk.delete_referential_action_desc, fk.update_referential_action_desc
FROM    sys.foreign_keys fk
JOIN    sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN    sys.columns cp ON cp.object_id = fkc.parent_object_id     AND cp.column_id = fkc.parent_column_id
JOIN    sys.columns cr ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id;

-- Find an object by name across types (table/view/proc/trigger/UDF):
SELECT name, type_desc, create_date, modify_date
FROM   sys.objects
WHERE  name LIKE '%User%'
ORDER  BY type_desc, name;

-- Cheat sheet of the most-used catalog views:
--   sys.databases                  one row per DB
--   sys.tables / sys.views         user tables / views
--   sys.columns / sys.types        column metadata + types
--   sys.indexes / sys.index_columns
--   sys.foreign_keys / sys.foreign_key_columns
--   sys.key_constraints            PK + UNIQUE
--   sys.check_constraints / sys.default_constraints
--   sys.procedures / sys.triggers / sys.objects
--   sys.parameters                 proc/UDF params
--   sys.schemas                    schema list
--   sys.partitions / sys.allocation_units / sys.dm_db_partition_stats
--   sys.sql_modules                source text for procs/views/UDFs/triggers
--   sys.synonyms                   SYNONYM objects
--   sys.sequences                  SEQUENCE objects
--   sys.principals / sys.database_principals / sys.server_principals
--   sys.permissions / sys.database_permissions
--   sys.configurations             sp_configure values


-- =============================================================================
-- 3) INFORMATION_SCHEMA VIEWS  (ANSI standard, cross-RDBMS)
-- =============================================================================
-- Pros: portable to other databases.
-- Cons: less complete than sys.*, no SQL Server-specific info (filegroups,
-- partition info, columnstore, etc.). Use when writing tool code that must
-- target multiple engines.

SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
FROM   INFORMATION_SCHEMA.TABLES;

SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM   INFORMATION_SCHEMA.COLUMNS
WHERE  TABLE_NAME = 'USERS';

SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME = 'USERS';
SELECT * FROM INFORMATION_SCHEMA.ROUTINES         WHERE ROUTINE_SCHEMA = 'dbo';


-- =============================================================================
-- 4) DYNAMIC MANAGEMENT VIEWS / FUNCTIONS  (sys.dm_*)
-- =============================================================================
-- "Live" runtime state of the engine. The DBA's microscope.

-- Sessions / requests / blocking (your live-debug trio):
SELECT * FROM sys.dm_exec_sessions WHERE is_user_process = 1;
SELECT * FROM sys.dm_exec_requests WHERE session_id <> @@SPID;
SELECT * FROM sys.dm_exec_connections;

-- The SQL text behind a running request (cross-apply with sys.dm_exec_sql_text):
SELECT r.session_id, r.status, r.wait_type, r.blocking_session_id, t.text
FROM   sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE  r.session_id <> @@SPID;

-- Plan cache top consumers:
--   sys.dm_exec_query_stats + sys.dm_exec_sql_text + sys.dm_exec_query_plan

-- Index usage / missing indexes:
--   sys.dm_db_index_usage_stats
--   sys.dm_db_index_physical_stats (fragmentation)
--   sys.dm_db_missing_index_details / _groups / _group_stats

-- Locking + transactions:
--   sys.dm_tran_locks
--   sys.dm_tran_active_transactions
--   sys.dm_tran_session_transactions

-- OS / waits / memory:
--   sys.dm_os_wait_stats        what is SQL waiting on?
--   sys.dm_os_performance_counters
--   sys.dm_os_memory_clerks
--   sys.dm_os_schedulers

-- IO:
--   sys.dm_io_virtual_file_stats(NULL, NULL)


-- =============================================================================
-- 5) BUILT-IN METADATA FUNCTIONS  (handy one-liners)
-- =============================================================================
SELECT OBJECT_ID('dbo.USERS')                  AS users_object_id,
       OBJECT_NAME(OBJECT_ID('dbo.USERS'))     AS users_name,
       OBJECT_SCHEMA_NAME(OBJECT_ID('dbo.USERS')) AS users_schema,
       DB_ID()        AS db_id,
       DB_NAME()      AS db_name,
       SCHEMA_ID('dbo') AS schema_id,
       SCHEMA_NAME(1) AS schema_name_1,
       USER_NAME()    AS db_user,
       SUSER_NAME()   AS server_login,
       HOST_NAME()    AS client_host,
       APP_NAME()     AS client_app,
       @@VERSION      AS sql_version,
       @@SERVERNAME   AS server_name,
       @@SPID         AS session_id,
       @@TRANCOUNT    AS open_trans;

-- OBJECTPROPERTY / COLUMNPROPERTY / INDEXPROPERTY / TYPEPROPERTY are useful
-- to test a single boolean fact:
SELECT OBJECTPROPERTY (OBJECT_ID('dbo.USERS'), 'TableHasPrimaryKey') AS has_pk,
       OBJECTPROPERTY (OBJECT_ID('dbo.USERS'), 'IsUserTable')         AS is_user_table,
       COLUMNPROPERTY (OBJECT_ID('dbo.USERS'), 'UserId', 'IsIdentity') AS userid_is_identity;


-- =============================================================================
-- 6) DBCC COMMANDS you actually use
-- =============================================================================
-- DBCC CHECKDB ('LEARNING') WITH NO_INFOMSGS, ALL_ERRORMSGS;   -- corruption check
-- DBCC CHECKIDENT ('dbo.USERS');                               -- show current identity
-- DBCC CHECKIDENT ('dbo.USERS', RESEED, 1000);                 -- reset identity
-- DBCC SHOW_STATISTICS ('dbo.USERS', 'PK_USERS');              -- histogram + density
-- DBCC SHRINKFILE  / DBCC SHRINKDATABASE                       -- avoid in prod (fragments indexes)
-- DBCC FREEPROCCACHE;                                          -- clear plan cache (dev only!)
-- DBCC DROPCLEANBUFFERS;                                       -- cold-cache test (dev only!)
-- DBCC OPENTRAN;                                               -- oldest active transaction
-- DBCC INPUTBUFFER (<spid>);                                   -- last batch a session ran


-- =============================================================================
-- 7) KEYBOARD SHORTCUTS  -  go fast
-- =============================================================================
/*
 SSMS (SQL Server Management Studio)
 -----------------------------------
   F5 / Ctrl+E         Execute query (selection if any)
   Ctrl+R              Toggle results pane
   Ctrl+T              Results as TEXT
   Ctrl+D              Results as GRID
   Ctrl+Shift+F        Results to FILE
   Ctrl+L              Display ESTIMATED execution plan
   Ctrl+M              Include ACTUAL execution plan (toggle, then F5)
   Ctrl+Shift+M        Specify template parameters
   Ctrl+K, Ctrl+C      Comment selection
   Ctrl+K, Ctrl+U      Uncomment selection
   Ctrl+]              Match brace
   Alt+F1              sp_help on the highlighted object (KILLER shortcut)
   Ctrl+1              sp_who
   Ctrl+2              sp_lock
   Ctrl+F1             Help / docs for the highlighted keyword
   F8                  Object Explorer
   F7                  Object Explorer Details
   Ctrl+N              New query window
   Ctrl+Shift+N        New project
   Ctrl+Alt+T          Template Explorer
   Ctrl+Shift+L        Lowercase selection
   Ctrl+Shift+U        UPPERCASE selection
   Ctrl+G              Go to line
   Ctrl+I              Incremental search
   Ctrl+F / Ctrl+H     Find / Find & Replace
   Ctrl+Shift+R        Refresh IntelliSense cache (after schema changes!)
   Ctrl+Shift+Q        Open Query Designer
   Ctrl+Tab            Switch between open windows
   Tab / Shift+Tab     Indent / outdent selection

   Custom (Tools -> Options -> Keyboard - heavily recommended):
     Ctrl+3   ->  SELECT TOP 100 * FROM
     Ctrl+4   ->  sp_helptext
     Ctrl+5   ->  sp_helpindex

 Azure Data Studio
 -----------------
   F5                  Run query
   Ctrl+Shift+E        Explain (estimated plan)
   Ctrl+M              Include actual plan
   Ctrl+Shift+P        Command palette  (everything lives here)
   Ctrl+P              Quick open file
   Ctrl+,              Settings
   Ctrl+`              Toggle terminal
   Ctrl+K Ctrl+C / U   Comment / uncomment

 VS Code + mssql extension
 -------------------------
   Ctrl+Shift+E        Execute query   (the mssql ext rebinding)
   Ctrl+Shift+C        Connect / change connection
   Ctrl+Shift+P        Command palette
   Ctrl+K Ctrl+C / U   Comment / uncomment
   Alt+Shift+F         Format document (with the mssql formatter)
   Ctrl+Click          Go to definition
   F12                 Go to definition
   Shift+F12           Find all references
   Ctrl+B              Toggle sidebar
   Ctrl+J              Toggle panel
   Ctrl+`              Toggle terminal
   Ctrl+/              Toggle line comment
   Ctrl+Shift+K        Delete line
   Alt+Up / Alt+Down   Move line up / down
   Shift+Alt+Up/Down   Copy line up / down
   Ctrl+D              Add next match to selection
   Ctrl+Shift+L        Select all occurrences
   Alt+Click           Multi-cursor
*/


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * sp_help / sp_helptext / sp_helpindex / sp_who2 / sp_lock  - daily drivers.
   * Prefer sys.* catalog views over legacy sysobjects/syscolumns.
   * INFORMATION_SCHEMA = portable, sys.* = complete; use sys.* for SQL Server.
   * sys.dm_* DMVs are the live-debug toolkit (requests, waits, plans, locks).
   * sp_executesql + QUOTENAME = safe dynamic SQL.
   * SSMS: Alt+F1 on a name = instant sp_help. Ctrl+L estimated plan, Ctrl+M actual.
   * VS Code mssql: Ctrl+Shift+E to run, F12 to navigate.
   ============================================================================= */
