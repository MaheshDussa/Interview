/* =============================================================================
   Script6.sql  -  VIEW vs TEMP TABLE vs CTE vs DERIVED TABLE
   -----------------------------------------------------------------------------
   DECISION FLOWCHART (the quick-pick cheat sheet):

     Start
       |
       v
     Do you need to reuse this across multiple queries / sessions?
       |-- YES --> Is it permanent business logic?
       |             |-- YES --> VIEW                 (saved query, no storage)
       |             '-- NO  --> TEMP TABLE           (materialized, supports indexes/stats)
       |
       '-- NO  --> Single-query use?
                     |-- Recursive?           --> CTE              (the ONLY recursive option)
                     |-- Complex / multi-step --> CTE              (readability + chained CTEs)
                     '-- Simple inline        --> DERIVED TABLE    (most compact)

   QUICK COMPARISON TABLE (interview-ready):

     Feature                 VIEW              TEMP TABLE (#t)     CTE                 DERIVED TABLE
     -----------------------  ----------------- ------------------- ------------------- -------------------
     Scope / lifetime        Permanent object  Session / batch     Single statement    Single statement
     Stores data?            No (just query)   Yes (tempdb)        No                  No
     Indexes / statistics    No (use indexed   Yes - real indexes  No                  No
                             view for perf)    and stats
     Recursion               No                No                  Yes (recursive CTE) No
     Reusable in same query  Yes               Yes                 Once (one statement) Once
     Best for                Reusable abstraction  Multi-pass /    Readability /        One-off subquery
                             of a query        large intermediates   recursion

   Other interview-worthy facts:
     * Non-indexed views are NOT materialized - they are macros (SQL Server expands
       them at parse time). Use WITH SCHEMABINDING + a UNIQUE CLUSTERED INDEX to
       create an "indexed view" that actually stores data.
     * #temp tables live in tempdb and are visible only to the session that created them.
       ##global temp tables are visible to all sessions until the last one closes.
     * Table variables (@t) are similar to temp tables but the optimizer estimates
       1 row (or actual rows with trace flag 2453 / SQL 2019+ deferred compilation) -
       fine for tiny sets, bad for large ones.
     * CTEs are NOT cached/materialized. If you reference the same CTE twice it is
       re-executed twice. Convert to a #temp table when re-use is expensive.
   ============================================================================= */


-- Scenario: list users whose role count is ABOVE the per-user average.

-- -----------------------------------------------------------------------------
-- Option 1: TEMP TABLE
-- Best when:
--   * The intermediate result is referenced multiple times.
--   * You want real indexes / statistics for the optimizer.
--   * The result is too big or too complex for a CTE to re-compute.
-- Notes:
--   * '#' prefix => session-scoped, auto-dropped at session end (but explicit
--     DROP TABLE is good practice in scripts that may be re-run in the same session).
--   * Lives in tempdb - I/O on tempdb is a common production bottleneck.
-- -----------------------------------------------------------------------------
IF OBJECT_ID('tempdb..#UserRoleCounts') IS NOT NULL DROP TABLE #UserRoleCounts;

CREATE TABLE #UserRoleCounts (
    UserId    INT NOT NULL PRIMARY KEY,        -- giving it a PK creates a useful index
    RoleCount INT NOT NULL
);

INSERT INTO #UserRoleCounts (UserId, RoleCount)
SELECT u.UserId,
       COUNT(ur.RoleId)                         -- COUNT(ur.RoleId) ignores NULLs => 0 for users with no roles
FROM [USERS] u
LEFT JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
GROUP BY u.UserId;

-- Materialize the average ONCE into a variable so the engine doesn't recompute it per row.
DECLARE @AvgRoles FLOAT = (SELECT AVG(CAST(RoleCount AS FLOAT)) FROM #UserRoleCounts);

SELECT *
FROM   #UserRoleCounts
WHERE  RoleCount > @AvgRoles;

DROP TABLE #UserRoleCounts;
GO


-- -----------------------------------------------------------------------------
-- Option 2: CTE  (Common Table Expression)
-- Best when:
--   * Single-statement use.
--   * You want named, layered, readable steps.
--   * You need RECURSION (the only option for hierarchical traversal).
-- Caveat:
--   * A CTE is NOT a temp table - referencing it twice (as we do below) means
--     SQL Server runs the underlying query TWICE. For expensive CTEs, switch
--     to a #temp table.
-- -----------------------------------------------------------------------------
WITH UserRoleCounts AS
(
    SELECT u.UserId,
           COUNT(ur.RoleId) AS RoleCount
    FROM [USERS] u
    LEFT JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
    GROUP BY u.UserId
)
SELECT *
FROM   UserRoleCounts
WHERE  RoleCount > (SELECT AVG(CAST(RoleCount AS FLOAT)) FROM UserRoleCounts);   -- 2nd reference => CTE re-executed
GO


-- -----------------------------------------------------------------------------
-- Option 3: DERIVED TABLE  (inline sub-SELECT in the FROM clause)
-- Best when:
--   * One-shot, simple inline query.
--   * You don't need to name / re-use the intermediate set.
-- Trade-off:
--   * Re-using the same logic (as required for the average here) forces you to
--     COPY THE SUB-QUERY - duplication that a CTE or #temp table avoids.
-- -----------------------------------------------------------------------------
SELECT *
FROM (
        SELECT u.UserId,
               COUNT(ur.RoleId) AS RoleCount
        FROM [USERS] u
        LEFT JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
        GROUP BY u.UserId
     ) AS urc
WHERE RoleCount > (
        SELECT AVG(CAST(RoleCount AS FLOAT))
        FROM (
                SELECT COUNT(ur2.RoleId) AS RoleCount       -- duplicated logic - the main downside
                FROM [USERS] u2
                LEFT JOIN [USER_ROLES] ur2 ON u2.UserId = ur2.UserId
                GROUP BY u2.UserId
             ) AS avg_calc
     );
GO


-- -----------------------------------------------------------------------------
-- Option 4: VIEW
-- Best when:
--   * The query is reused across many sessions / reports.
--   * You want to expose a stable, simplified API on top of a complex schema.
--   * You want to grant SELECT on a subset of columns/rows without exposing
--     the base tables (security boundary).
-- Notes:
--   * A normal view is NOT stored - it is expanded into the calling query.
--   * For caching, create an INDEXED VIEW: WITH SCHEMABINDING + UNIQUE CLUSTERED INDEX.
--   * CREATE OR ALTER lets the script be re-run safely.
-- -----------------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.vw_UserRoleCounts
AS
SELECT u.UserId,
       u.FirstName,
       u.LastName,
       COUNT(ur.RoleId) AS RoleCount
FROM [USERS] u
LEFT JOIN [USER_ROLES] ur ON u.UserId = ur.UserId
GROUP BY u.UserId, u.FirstName, u.LastName;
GO

-- Use the view (now reusable from anywhere with SELECT permission).
SELECT *
FROM   dbo.vw_UserRoleCounts
WHERE  RoleCount > (SELECT AVG(CAST(RoleCount AS FLOAT)) FROM dbo.vw_UserRoleCounts);
GO


-- -----------------------------------------------------------------------------
-- BONUS: RECURSIVE CTE  (the ONE thing only a CTE can do).
-- Classic example: generate a numbers table 1..10 without a base table.
-- Anatomy:
--     WITH cte AS (
--         <anchor member>           -- runs once, seeds the recursion
--         UNION ALL
--         <recursive member>        -- references cte, runs until it returns 0 rows
--     )
-- Always add OPTION (MAXRECURSION n) - default cap is 100, raise/lower as needed.
-- -----------------------------------------------------------------------------
WITH Numbers AS
(
    SELECT 1 AS n                          -- anchor
    UNION ALL
    SELECT n + 1                           -- recursive step
    FROM Numbers
    WHERE n < 10                           -- termination predicate is MANDATORY
)
SELECT n FROM Numbers
OPTION (MAXRECURSION 100);
GO
