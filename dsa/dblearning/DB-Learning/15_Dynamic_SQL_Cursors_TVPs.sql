/* =============================================================================
   15_Dynamic_SQL_Cursors_TVPs.sql
   -----------------------------------------------------------------------------
   1) Dynamic SQL with sp_executesql (and the SQL-injection trap).
   2) Cursors - and why to AVOID them; set-based replacements.
   3) Table-Valued Parameters (TVPs) and User-Defined Table Types.
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) DYNAMIC SQL
-- =============================================================================
-- Two flavors:
--   EXEC ('...')            -> string concat, NO parameters - DANGEROUS.
--   sp_executesql N'...', N'@p type', @p = ...  -> PARAMETERIZED, plan-cacheable.
--
-- Always prefer sp_executesql + QUOTENAME() for identifiers + parameters
-- for values. Never concatenate user input directly.

-- BAD (SQL injection-prone) - shown only to recognize the anti-pattern:
DECLARE @badName NVARCHAR(50) = N'O''Hara';
DECLARE @badSql  NVARCHAR(MAX) = N'SELECT * FROM dbo.USERS WHERE LastName = ''' + @badName + N'''';
-- EXEC (@badSql);    -- breaks on the apostrophe, and worse: ' OR 1=1 -- payloads.

-- GOOD - parameterized:
DECLARE @lastName NVARCHAR(50) = N'Doe';
DECLARE @sql NVARCHAR(MAX) =
    N'SELECT UserId, FirstName, LastName FROM dbo.USERS WHERE LastName = @ln';

EXEC sp_executesql
        @sql,
        N'@ln NVARCHAR(50)',
        @ln = @lastName;

-- Dynamic IDENTIFIERS (table / column names) cannot be parameters - quote
-- them with QUOTENAME() to escape brackets safely:
DECLARE @tbl SYSNAME = N'USERS';
DECLARE @dyn NVARCHAR(MAX) =
    N'SELECT TOP (5) * FROM dbo.' + QUOTENAME(@tbl);
EXEC sp_executesql @dyn;


-- =============================================================================
-- 2) CURSORS - and how to avoid them
-- =============================================================================
-- A CURSOR processes rows ONE AT A TIME. SQL Server is a set-based engine,
-- so cursors are almost always slower and use more locks/tempdb than the
-- set-based equivalent.
--
-- When a cursor is genuinely justified:
--   * Calling a stored procedure that requires scalar parameters per row.
--   * Running heterogeneous DDL per object (DBA scripts).
--   * Sending data to an external system one row at a time.
--
-- Cursor options (least overhead first): LOCAL FAST_FORWARD READ_ONLY.

DECLARE @uid INT, @name NVARCHAR(200);

DECLARE cur CURSOR LOCAL FAST_FORWARD READ_ONLY FOR
    SELECT UserId, CONCAT(FirstName, N' ', LastName)
    FROM   dbo.USERS
    WHERE  IsActive = 1;

OPEN cur;
FETCH NEXT FROM cur INTO @uid, @name;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- Per-row work here. EXAMPLE: PRINT N'User: ' + @name;
    FETCH NEXT FROM cur INTO @uid, @name;
END
CLOSE cur;
DEALLOCATE cur;

-- Set-based equivalent (always preferred when possible):
-- SELECT UserId, CONCAT(FirstName, N' ', LastName) FROM dbo.USERS WHERE IsActive = 1;


-- =============================================================================
-- 3) USER-DEFINED TABLE TYPE + TABLE-VALUED PARAMETER (TVP)
-- =============================================================================
-- Best way to pass a SET of rows from app -> SQL in ONE call.
-- Replaces "loop and call proc N times" anti-patterns and giant CSV parsing.
--
-- Steps:
--   1. CREATE TYPE ... AS TABLE (the schema).
--   2. CREATE PROC with a parameter of that type, READONLY (mandatory).
--   3. Caller declares a variable of that type, populates it, passes it in.

IF EXISTS (SELECT 1 FROM sys.types WHERE name = 'UserUpsertType' AND is_table_type = 1)
    DROP TYPE dbo.UserUpsertType;

CREATE TYPE dbo.UserUpsertType AS TABLE
(
    Email     NVARCHAR(100) NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NULL,
    LastName  NVARCHAR(100) NULL
);
GO

CREATE OR ALTER PROCEDURE dbo.UpsertUsersBulk
    @Users dbo.UserUpsertType READONLY              -- TVPs are ALWAYS readonly
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.USERS AS tgt
    USING @Users          AS src
       ON tgt.Email = src.Email
    WHEN MATCHED THEN
        UPDATE SET FirstName = src.FirstName, LastName = src.LastName
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (FirstName, LastName, Email, IsActive)
        VALUES (src.FirstName, src.LastName, src.Email, 1);
END
GO

-- Caller-side usage:
DECLARE @batch dbo.UserUpsertType;
INSERT INTO @batch (Email, FirstName, LastName) VALUES
('a@x.com', 'A', 'One'),
('b@x.com', 'B', 'Two'),
('c@x.com', 'C', 'Three');

EXEC dbo.UpsertUsersBulk @Users = @batch;


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * Dynamic SQL: sp_executesql + parameters + QUOTENAME for identifiers.
     NEVER concatenate user input.
   * Cursors: avoid; if unavoidable use LOCAL FAST_FORWARD READ_ONLY.
   * TVPs: best way to pass a row set into a proc; READONLY required;
     prefer over comma-separated strings / XML blobs.
   ============================================================================= */
