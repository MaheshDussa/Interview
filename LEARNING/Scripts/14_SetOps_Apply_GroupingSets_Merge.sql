/* =============================================================================
   14_SetOps_Apply_GroupingSets_Merge.sql
   -----------------------------------------------------------------------------
   Powerful T-SQL constructs that show up constantly in real work AND in
   interviews:
       1) Set operators       : UNION / UNION ALL / INTERSECT / EXCEPT
       2) APPLY operators     : CROSS APPLY / OUTER APPLY (a.k.a. lateral join)
       3) Grouping extensions : GROUPING SETS / ROLLUP / CUBE / GROUPING_ID
       4) MERGE / upserts     : single statement INSERT + UPDATE + DELETE
       5) OUTPUT clause       : capture rows affected by DML
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) SET OPERATORS
-- =============================================================================
-- Rules:
--   * Both sides must have the SAME number of columns and COMPATIBLE types.
--   * Column names come from the FIRST query.
--   * UNION removes duplicates (sort/hash distinct - costs CPU).
--   * UNION ALL keeps duplicates - cheaper, prefer when dupes are impossible/ok.
--   * INTERSECT / EXCEPT compare ALL columns, NULL = NULL for this purpose.

-- All users (no dedup):
SELECT FirstName, LastName FROM dbo.USERS
UNION ALL
SELECT 'Guest', 'User';

-- Distinct emails across multiple sources:
SELECT Email FROM dbo.USERS
UNION
SELECT 'imported@example.com';

-- Common values (set intersection):
SELECT UserId FROM dbo.USERS
INTERSECT
SELECT UserId FROM dbo.USER_ROLES;

-- Users that have NO role (anti-join via set difference):
SELECT UserId FROM dbo.USERS
EXCEPT
SELECT UserId FROM dbo.USER_ROLES;


-- =============================================================================
-- 2) CROSS APPLY / OUTER APPLY  (LATERAL JOIN)
-- =============================================================================
-- APPLY lets the right-hand side reference the left row. Use cases:
--   * Call a table-valued function per outer row.
--   * Run a correlated TOP-N per group (the classic "top 2 orders per customer").
--   * Pivot a JSON column out per row.
--
-- CROSS APPLY = INNER JOIN style (drops left rows with no matches).
-- OUTER APPLY = LEFT  JOIN style (keeps left rows, NULL columns when no match).

-- "Top 2 most recent role assignments per user":
SELECT u.UserId, u.FirstName, x.RoleId
FROM   dbo.USERS u
CROSS APPLY (
    SELECT TOP (2) ur.RoleId
    FROM   dbo.USER_ROLES ur
    WHERE  ur.UserId = u.UserId
    ORDER  BY ur.RoleId DESC
) AS x;

-- Same, but keep users that have NO roles:
SELECT u.UserId, u.FirstName, x.RoleId
FROM   dbo.USERS u
OUTER APPLY (
    SELECT TOP (2) ur.RoleId
    FROM   dbo.USER_ROLES ur
    WHERE  ur.UserId = u.UserId
    ORDER  BY ur.RoleId DESC
) AS x;


-- =============================================================================
-- 3) GROUPING SETS / ROLLUP / CUBE
-- =============================================================================
-- ONE query returns MULTIPLE levels of grouping. Saves trips and UNION ALLs.
--
-- GROUPING SETS : explicit list of grouping combinations.
-- ROLLUP        : hierarchical totals - subtotals at each level + grand total.
-- CUBE          : every possible combination of the listed columns.
-- GROUPING_ID() : returns a bitmask telling you WHICH grouping the row belongs to.

-- Sample mini-table:
IF OBJECT_ID('tempdb..#SalesByDeptYear') IS NOT NULL DROP TABLE #SalesByDeptYear;
SELECT Department = d.Department, [Year] = y.[Year], Amount = CAST(d.amt * y.mult AS DECIMAL(12,2))
INTO   #SalesByDeptYear
FROM  (VALUES ('Sales', 1000),('IT', 500),('HR', 300)) d(Department, amt)
CROSS JOIN (VALUES (2024, 1.0),(2025, 1.2),(2026, 1.4)) y([Year], mult);

-- ROLLUP: yearly subtotals per department + dept totals + grand total.
SELECT Department, [Year], SUM(Amount) AS Total,
       GROUPING_ID(Department, [Year]) AS gid
FROM   #SalesByDeptYear
GROUP BY ROLLUP (Department, [Year])
ORDER BY GROUPING_ID(Department, [Year]), Department, [Year];

-- GROUPING SETS: only the two levels you actually want.
SELECT Department, [Year], SUM(Amount) AS Total
FROM   #SalesByDeptYear
GROUP BY GROUPING SETS ( (Department), ([Year]), () );    -- () = grand total

-- CUBE: every combination (Dept, Year, Dept+Year, grand total).
SELECT Department, [Year], SUM(Amount) AS Total
FROM   #SalesByDeptYear
GROUP BY CUBE (Department, [Year]);


-- =============================================================================
-- 4) MERGE  -  upsert in one statement
-- =============================================================================
-- Single statement for "INSERT new rows, UPDATE changed rows, optionally
-- DELETE rows that disappeared from the source".
--
-- Interview caveats (MERGE has historically had bugs - know them):
--   * Always include a SEMICOLON at the end (mandatory).
--   * Watch for filter-index / Halloween problems on older versions.
--   * On heavy-concurrency workloads, prefer explicit IF EXISTS / INSERT /
--     UPDATE pattern with appropriate locking.

IF OBJECT_ID('dbo.UserStaging', 'U') IS NOT NULL DROP TABLE dbo.UserStaging;
CREATE TABLE dbo.UserStaging
(
    Email     NVARCHAR(100) NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NULL,
    LastName  NVARCHAR(100) NULL
);

INSERT INTO dbo.UserStaging (Email, FirstName, LastName) VALUES
('john.doe@email.com', 'Johnny', 'Doe'),     -- existing user (updated first name)
('new.person@email.com', 'New',    'Person');-- brand-new user

MERGE dbo.USERS AS tgt
USING dbo.UserStaging AS src
   ON tgt.Email = src.Email
WHEN MATCHED AND (tgt.FirstName <> src.FirstName OR ISNULL(tgt.LastName,'') <> ISNULL(src.LastName,''))
     THEN UPDATE SET tgt.FirstName = src.FirstName,
                     tgt.LastName  = src.LastName
WHEN NOT MATCHED BY TARGET
     THEN INSERT (FirstName, LastName, Email, IsActive)
          VALUES (src.FirstName, src.LastName, src.Email, 1)
-- WHEN NOT MATCHED BY SOURCE THEN DELETE   -- optional sync: delete users not in staging
OUTPUT $action AS Action,                   -- 'INSERT' / 'UPDATE' / 'DELETE'
       inserted.UserId, inserted.Email;
;


-- =============================================================================
-- 5) OUTPUT clause - capture what DML did
-- =============================================================================
-- INSERT / UPDATE / DELETE / MERGE all support OUTPUT. Returns:
--   inserted.*  -> rows after the change (NULL for DELETE)
--   deleted.*   -> rows before the change (NULL for INSERT)
--
-- Use OUTPUT ... INTO @tableVar to capture for further processing (audit, ETL).

DECLARE @audit TABLE (UserId INT, OldEmail NVARCHAR(100), NewEmail NVARCHAR(100));

UPDATE dbo.USERS
   SET Email = LOWER(Email)
OUTPUT inserted.UserId, deleted.Email, inserted.Email
INTO   @audit
WHERE  UserId IN (1,2,3);

SELECT * FROM @audit;


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * UNION removes dupes (slower); UNION ALL keeps them (faster, prefer when safe).
   * EXCEPT / INTERSECT compare ALL columns and treat NULL=NULL.
   * CROSS APPLY = correlated table expression; perfect for top-N-per-group.
   * GROUPING SETS / ROLLUP / CUBE return multiple aggregation levels in ONE pass.
   * MERGE is concise but has gotchas; OUTPUT $action is great for audit.
   ============================================================================= */
