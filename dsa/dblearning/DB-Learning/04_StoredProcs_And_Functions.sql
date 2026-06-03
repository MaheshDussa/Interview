/* =============================================================================
   Script4.sql  -  Stored Procedures vs Functions in T-SQL
   -----------------------------------------------------------------------------
   QUICK DECISION MATRIX (interview cheat sheet):

     Need To...                                   Use This                Why
     -------------------------------------------- ----------------------  ----------------------------------------------
     Modify data (INSERT/UPDATE/DELETE)           Stored Procedure        Only object that allows DML
     Return a table from a parameterized query    Inline TVF              Best perf - optimizer inlines it like a view
     Complex table logic (variables/loops)        Multi-Statement TVF     Last resort - slower than iTVF (no inlining)
     Simple calculation (string / date)           Scalar Function         OK for utilities, AVOID in WHERE / JOIN
     Calculation in WHERE on a large table        CTE or JOIN             Scalar UDF in predicate kills performance
     Multiple result sets                         Stored Procedure        Only option for multiple result sets
     Transaction control (BEGIN/COMMIT/ROLLBACK)  Stored Procedure        Functions cannot start/commit transactions

   KEY DIFFERENCES (Stored Procedure  vs  Function):
     * SP can do DML, DDL, transactions, TRY/CATCH; FN cannot modify data.
     * SP is invoked with EXEC; FN is invoked inside a SELECT / expression.
     * SP can return 0..N result sets and an INT return code; FN returns a
       single scalar OR a single table.
     * SP supports OUTPUT parameters; FN does not.
     * Scalar UDFs in WHERE / JOIN force row-by-row evaluation in older
       versions (pre SQL 2019 "Scalar UDF Inlining") -> common perf trap.
   ============================================================================= */


-- =============================================================================
-- STORED PROCEDURES
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1) Basic SP - no parameters. Returns a result set of active users.
--    CREATE OR ALTER is idiomatic for re-runnable deployment scripts
--    (works on SQL Server 2016 SP1+).
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetActiveUsers
AS
BEGIN
    SET NOCOUNT ON;                         -- Suppresses "(n rows affected)" chatter -> less network traffic, avoids triggering some ORMs

    SELECT UserId, FirstName, LastName, Email
    FROM dbo.USERS
    WHERE IsActive = 1;
END
GO


-- -----------------------------------------------------------------------------
-- 2) SP with INPUT parameter.
--    Parameters are strongly typed and naturally prevent SQL injection
--    (vs string concatenation into dynamic SQL).
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetUsersByRole
    @RoleName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT u.UserId, u.FirstName, u.LastName, u.Email
    FROM dbo.USERS u
    INNER JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
    INNER JOIN [ROLES]      r  ON ur.RoleId = r.RoleId
    WHERE r.RoleName = @RoleName;
END
GO


-- -----------------------------------------------------------------------------
-- 3) SP with OUTPUT parameter.
--    Useful when the caller needs a single scalar back (count, generated id ...).
--    Caller invokes it like:
--        DECLARE @cnt INT;
--        EXEC dbo.GetUserCountByRole @RoleName = 'Developer', @UserCount = @cnt OUTPUT;
--        SELECT @cnt;
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.GetUserCountByRole
    @RoleName  NVARCHAR(50),
    @UserCount INT OUTPUT                     -- OUTPUT keyword is required on BOTH the definition and the call
AS
BEGIN
    SET NOCOUNT ON;

    SELECT @UserCount = COUNT(*)
    FROM dbo.USERS u
    INNER JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
    INNER JOIN [ROLES]      r  ON ur.RoleId = r.RoleId
    WHERE r.RoleName = @RoleName;
END
GO


-- -----------------------------------------------------------------------------
-- 4) SP with TRY / CATCH + TRANSACTION  (the "production-ready" template).
--
--    Pattern interviewers expect:
--      * SET XACT_ABORT ON  -> any runtime error rolls back the whole batch
--      * BEGIN TRAN inside TRY
--      * COMMIT at the end of TRY
--      * ROLLBACK inside CATCH (only if a transaction is still open)
--      * Re-raise the error with THROW so the caller sees it
--
--    XACT_STATE() returns:
--        1  = healthy, committable transaction
--        0  = no open transaction
--       -1  = doomed (uncommittable) transaction -> must ROLLBACK
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.AssignRoleToUser
    @UserId INT,
    @RoleId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;                          -- forces ROLLBACK on any runtime error / disconnect

    BEGIN TRY
        -- Input validation - fail fast with a clear error.
        IF NOT EXISTS (SELECT 1 FROM dbo.USERS WHERE UserId = @UserId)
            THROW 50001, 'User does not exist.', 1;          -- error number must be >= 50000 for user-defined

        IF NOT EXISTS (SELECT 1 FROM dbo.ROLES WHERE RoleId = @RoleId)
            THROW 50002, 'Role does not exist.', 1;

        BEGIN TRAN;

            -- Idempotent insert: only add the mapping if it isn't already there
            -- (avoids PK violation on the composite (UserId, RoleId) key).
            IF NOT EXISTS (SELECT 1 FROM dbo.USER_ROLES WHERE UserId = @UserId AND RoleId = @RoleId)
            BEGIN
                INSERT INTO dbo.USER_ROLES (UserId, RoleId)
                VALUES (@UserId, @RoleId);
            END

        COMMIT TRAN;
    END TRY
    BEGIN CATCH
        -- Only roll back if there is still an open / doomed transaction.
        IF XACT_STATE() <> 0
            ROLLBACK TRAN;

        -- Optional: log the error somewhere durable before re-raising.
        --   INSERT INTO dbo.ErrorLog (...) VALUES (ERROR_NUMBER(), ERROR_MESSAGE(), ...);

        -- Re-raise the ORIGINAL error to the caller. THROW (no args) preserves
        -- the original error number, message, line, severity. RAISERROR would
        -- lose that fidelity and is considered legacy.
        THROW;
    END CATCH
END
GO


-- =============================================================================
-- FUNCTIONS
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 5) SCALAR FUNCTION - returns a single value.
--    Convenient, but WATCH OUT: a scalar UDF used in SELECT / WHERE on a
--    large table can serialize the plan and tank performance. SQL Server 2019+
--    "Scalar UDF Inlining" helps, but only when many restrictions are met.
--    Prefer expressing the logic inline or via an inline TVF + APPLY.
--
--    WITH SCHEMABINDING:
--      * Locks the referenced objects so they cannot be altered/dropped
--        without first dropping the function.
--      * Required for the optimizer to mark the UDF as deterministic /
--        non-data-accessing, which is a prerequisite for inlining.
-- -----------------------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.GetFullName
(
    @UserId INT
)
RETURNS NVARCHAR(201)                 -- 100 + 1 (space) + 100
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @FullName NVARCHAR(201);

    SELECT @FullName = CONCAT(FirstName, N' ', LastName)   -- CONCAT treats NULL as ''
    FROM dbo.USERS
    WHERE UserId = @UserId;

    RETURN @FullName;
END
GO


-- -----------------------------------------------------------------------------
-- 6) INLINE TABLE-VALUED FUNCTION (iTVF)  - THE PERFORMANT ONE.
--    Body is a single RETURN ( SELECT ... ). The optimizer "inlines" it
--    into the calling query, exactly like a parameterized view.
--    Use it whenever you need a parameterized result set.
-- -----------------------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.GetUsersByStatus
(
    @IsActive BIT
)
RETURNS TABLE
AS
RETURN
(
    SELECT UserId, FirstName, LastName, Email
    FROM dbo.USERS
    WHERE IsActive = @IsActive
);
GO


-- -----------------------------------------------------------------------------
-- 7) MULTI-STATEMENT TABLE-VALUED FUNCTION (MSTVF)  - the slow cousin.
--    Used only when the table result needs IF/ELSE, loops, or multiple
--    INSERT steps. Optimizer cannot "see inside" -> often estimates 1 row
--    (or 100 in newer versions) -> bad plans on large data.
--    Rewrite as an iTVF whenever possible.
-- -----------------------------------------------------------------------------
CREATE OR ALTER FUNCTION dbo.GetUsersWithRoleFlag
(
    @IsActive BIT
)
RETURNS @Result TABLE
(
    UserId    INT,
    FullName  NVARCHAR(201),
    HasRole   BIT
)
AS
BEGIN
    INSERT INTO @Result (UserId, FullName, HasRole)
    SELECT u.UserId,
           CONCAT(u.FirstName, N' ', u.LastName),
           CASE WHEN EXISTS (SELECT 1 FROM dbo.USER_ROLES ur WHERE ur.UserId = u.UserId)
                THEN 1 ELSE 0 END
    FROM dbo.USERS u
    WHERE u.IsActive = @IsActive;

    RETURN;
END
GO


-- =============================================================================
-- USAGE / DEMO BLOCK  (also illustrates client-side TRY/CATCH around EXEC)
-- =============================================================================
SET NOCOUNT ON;

-- Call SP with no params.
EXEC dbo.GetActiveUsers;

-- Call SP with an input param (named arguments are clearer than positional).
EXEC dbo.GetUsersByRole @RoleName = N'Developer';

-- Call SP with an OUTPUT param.
DECLARE @cnt INT;
EXEC dbo.GetUserCountByRole @RoleName = N'Developer', @UserCount = @cnt OUTPUT;
SELECT @cnt AS DeveloperCount;

-- Wrap the transactional SP in TRY/CATCH on the caller side too.
BEGIN TRY
    EXEC dbo.AssignRoleToUser @UserId = 1, @RoleId = 1;     -- safe re-run (idempotent)
    EXEC dbo.AssignRoleToUser @UserId = 9999, @RoleId = 1;  -- will THROW 50001
END TRY
BEGIN CATCH
    SELECT
        ERROR_NUMBER()   AS ErrorNumber,
        ERROR_SEVERITY() AS ErrorSeverity,
        ERROR_STATE()    AS ErrorState,
        ERROR_LINE()     AS ErrorLine,
        ERROR_PROCEDURE()AS ErrorProcedure,
        ERROR_MESSAGE()  AS ErrorMessage;
END CATCH;

-- Functions are called INSIDE a SELECT, not via EXEC.
SELECT dbo.GetFullName(1) AS FullName;                  -- scalar UDF
SELECT * FROM dbo.GetUsersByStatus(1);                  -- iTVF - behaves like a parameterized view
SELECT * FROM dbo.GetUsersWithRoleFlag(1);              -- MSTVF
GO


