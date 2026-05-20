/* =============================================================================
   Script5.sql  -  TRIGGERS in T-SQL
   -----------------------------------------------------------------------------
   What is a trigger?
     A trigger is a special stored procedure that the database engine fires
     AUTOMATICALLY in response to an event on a table or view.

   T-SQL trigger flavors:
     * DML triggers  : fire on INSERT / UPDATE / DELETE.
         - AFTER  (a.k.a. FOR)  - run AFTER the DML completes and constraints pass.
         - INSTEAD OF           - replace the DML; commonly used on views to make them updatable.
     * DDL triggers  : fire on CREATE / ALTER / DROP (schema changes). Server- or DB-scoped.
     * LOGON triggers: fire when a session is established.

   Magic pseudo-tables available inside a DML trigger:
     * inserted  -> the NEW rows  (populated for INSERT and UPDATE)
     * deleted   -> the OLD rows  (populated for DELETE and UPDATE)
     * UPDATE  : both 'inserted' (after image) and 'deleted' (before image) are populated.
     * They live only for the duration of the trigger and have the same schema as the base table.

   Interview talking points / gotchas:
     * Triggers fire ONCE PER STATEMENT, not once per row. Write SET-BASED code.
       Never assume "inserted" has only one row.
     * Triggers run inside the same TRANSACTION as the firing statement -
       a ROLLBACK inside the trigger rolls back the original DML too.
     * Always SET NOCOUNT ON to avoid extra row-count messages confusing ORMs.
     * Avoid heavy logic / external calls in triggers - they hold locks and
       lengthen the transaction.
     * Recursive / nested triggers (server settings RECURSIVE_TRIGGERS,
       nested triggers) can cause surprising loops - keep them off unless needed.
     * Prefer auditing via temporal tables / CDC for high-volume systems;
       triggers are easier but harder to scale.
   ============================================================================= */


-- -----------------------------------------------------------------------------
-- 0) AUDIT LOG TABLES  (created first so the triggers below have a target).
--    Note the Operation column (I/U/D) and a single timestamp column - keeps
--    the schema uniform and lets all three logs be merged easily.
-- -----------------------------------------------------------------------------
IF OBJECT_ID('dbo.UserInsertLog', 'U') IS NULL
CREATE TABLE dbo.UserInsertLog (
    LogId        INT IDENTITY(1,1) PRIMARY KEY,
    UserId       INT           NOT NULL,
    FirstName    NVARCHAR(100) NULL,
    LastName     NVARCHAR(100) NULL,
    Email        NVARCHAR(100) NULL,
    InsertedDate DATETIME      NOT NULL DEFAULT GETDATE(),
    InsertedBy   SYSNAME       NOT NULL DEFAULT SUSER_SNAME()    -- captures the login that ran the INSERT
);
GO

IF OBJECT_ID('dbo.UserUpdateLog', 'U') IS NULL
CREATE TABLE dbo.UserUpdateLog (
    LogId        INT IDENTITY(1,1) PRIMARY KEY,
    UserId       INT           NOT NULL,
    OldFirstName NVARCHAR(100) NULL,
    OldLastName  NVARCHAR(100) NULL,
    OldEmail     NVARCHAR(100) NULL,
    NewFirstName NVARCHAR(100) NULL,
    NewLastName  NVARCHAR(100) NULL,
    NewEmail     NVARCHAR(100) NULL,
    UpdatedDate  DATETIME      NOT NULL DEFAULT GETDATE(),
    UpdatedBy    SYSNAME       NOT NULL DEFAULT SUSER_SNAME()
);
GO

IF OBJECT_ID('dbo.UserDeleteLog', 'U') IS NULL
CREATE TABLE dbo.UserDeleteLog (
    LogId       INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT           NOT NULL,
    FirstName   NVARCHAR(100) NULL,
    LastName    NVARCHAR(100) NULL,
    Email       NVARCHAR(100) NULL,
    DeletedDate DATETIME      NOT NULL DEFAULT GETDATE(),
    DeletedBy   SYSNAME       NOT NULL DEFAULT SUSER_SNAME()
);
GO


-- -----------------------------------------------------------------------------
-- 1) AFTER INSERT trigger
--    Fires once per INSERT statement, AFTER constraints have been checked.
--    'inserted' holds every new row from the statement (not just one).
-- -----------------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.AfterUserInsert
ON dbo.USERS
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;                              -- suppress extra row counts (ORM-friendly + tiny perf win)

    -- Short-circuit if nothing was actually inserted (rare but defensive).
    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    -- Set-based copy from the 'inserted' pseudo-table into the audit log.
    INSERT INTO dbo.UserInsertLog (UserId, FirstName, LastName, Email)
    SELECT UserId, FirstName, LastName, Email
    FROM inserted;
END
GO


-- -----------------------------------------------------------------------------
-- 2) AFTER UPDATE trigger
--    For UPDATE, BOTH 'inserted' (new values) and 'deleted' (old values) are
--    populated and can be JOINed on the primary key to see what changed.
--    UPDATE(column) and COLUMNS_UPDATED() can tell you WHICH columns were touched.
-- -----------------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.AfterUserUpdate
ON dbo.USERS
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    -- Optional optimization: only log when one of the audited columns actually changed.
    -- UPDATE(col) returns TRUE if the col was in the SET list (even if value didn't change).
    IF NOT (UPDATE(FirstName) OR UPDATE(LastName) OR UPDATE(Email))
        RETURN;

    INSERT INTO dbo.UserUpdateLog (
        UserId,
        OldFirstName, OldLastName, OldEmail,
        NewFirstName, NewLastName, NewEmail)
    SELECT  i.UserId,
            d.FirstName, d.LastName, d.Email,           -- OLD image
            i.FirstName, i.LastName, i.Email            -- NEW image
    FROM inserted i
    INNER JOIN deleted d ON d.UserId = i.UserId;        -- join on PK to pair before/after rows
END
GO


-- -----------------------------------------------------------------------------
-- 3) AFTER DELETE trigger
--    Only 'deleted' is populated. Useful for soft-audit ("who removed what?").
-- -----------------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.AfterUserDelete
ON dbo.USERS
AFTER DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    INSERT INTO dbo.UserDeleteLog (UserId, FirstName, LastName, Email)
    SELECT UserId, FirstName, LastName, Email
    FROM deleted;
END
GO


-- -----------------------------------------------------------------------------
-- 4) BONUS: combined AFTER INSERT, UPDATE, DELETE trigger.
--    One trigger handling all three events. Detect WHICH event fired by
--    checking the row counts of the magic tables:
--         inserted only            -> INSERT
--         deleted  only            -> DELETE
--         BOTH inserted & deleted  -> UPDATE
-- -----------------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.AuditUserAll
ON dbo.USERS
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @op CHAR(1) =
        CASE
            WHEN EXISTS (SELECT 1 FROM inserted) AND EXISTS (SELECT 1 FROM deleted) THEN 'U'
            WHEN EXISTS (SELECT 1 FROM inserted)                                    THEN 'I'
            WHEN EXISTS (SELECT 1 FROM deleted)                                     THEN 'D'
            ELSE NULL                                                                       -- empty DML (e.g., UPDATE with no matching rows)
        END;

    IF @op IS NULL RETURN;

    -- Place to forward into a single unified audit table, raise an event, etc.
    -- (kept as a no-op here so the trigger compiles cleanly).
END
GO


-- -----------------------------------------------------------------------------
-- 5) BONUS: INSTEAD OF trigger on a VIEW (makes the view updatable).
--    The view aggregates two columns; without INSTEAD OF, INSERTs would fail.
--    INSTEAD OF triggers REPLACE the original DML - the engine never runs the
--    original statement, only what's inside the trigger.
-- -----------------------------------------------------------------------------
-- (Template only - uncomment after creating dbo.vw_ActiveUsers.)
--CREATE OR ALTER TRIGGER dbo.vw_ActiveUsers_InsteadOfInsert
--ON dbo.vw_ActiveUsers
--INSTEAD OF INSERT
--AS
--BEGIN
--    SET NOCOUNT ON;
--    INSERT INTO dbo.USERS (FirstName, LastName, Email, IsActive)
--    SELECT FirstName, LastName, Email, 1
--    FROM inserted;
--END
--GO


-- -----------------------------------------------------------------------------
-- 6) BONUS: enabling / disabling and inspecting triggers
-- -----------------------------------------------------------------------------
--   DISABLE TRIGGER dbo.AfterUserInsert ON dbo.USERS;
--   ENABLE  TRIGGER dbo.AfterUserInsert ON dbo.USERS;
--   DISABLE TRIGGER ALL ON dbo.USERS;          -- nuclear option, prefer per-trigger
--
--   -- List triggers on a table:
--   SELECT name, is_disabled, is_instead_of_trigger
--   FROM   sys.triggers
--   WHERE  parent_id = OBJECT_ID('dbo.USERS');
GO

