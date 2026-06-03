/* =============================================================================
   01_Microsoft_Interview_150_Scenarios.sql
   -----------------------------------------------------------------------------
   150+ scenario-style SQL Server interview questions WITH runnable answers.
   Schema assumed: dbo.USERS, dbo.ROLES, dbo.USER_ROLES, dbo.ROLE_PERMISSIONS,
                   dbo.ROLE_PERMISSION_MAPPING, dbo.SalesData
   (created by Scripts/01_Schema_RBAC_Bootstrap.sql).

   For self-contained demos, throwaway #temp tables and table variables are
   created inside individual sections.

   How to use:
     * Read the QUESTION comment.
     * Try to write the query yourself.
     * Compare with the ANSWER below.
     * Most answers include WHY notes - those are the interview talking points.

   Sections:
     A. SELECT / WHERE / NULL / CASE                 (Q1-15)
     B. Aggregates, GROUP BY, HAVING                 (Q16-30)
     C. Joins (every flavor + anti / semi / self)    (Q31-45)
     D. Subqueries + CTEs + Recursion                (Q46-60)
     E. Window functions                             (Q61-80)
     F. PIVOT / UNPIVOT / Conditional aggregation    (Q81-90)
     G. Indexes / SARGability / Plans                (Q91-100)
     H. Transactions, isolation, locking             (Q101-110)
     I. Stored procedures, functions, triggers       (Q111-120)
     J. Performance tuning, stats, partitioning      (Q121-130)
     K. Security, RLS, DDM                           (Q131-140)
     L. Tricky / classic puzzles                     (Q141-160)
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- A. SELECT / WHERE / NULL / CASE  (Q1-Q15)
-- =============================================================================

-- Q1. Return all active users sorted by last name then first name.
SELECT UserId, FirstName, LastName, Email
FROM   dbo.USERS
WHERE  IsActive = 1
ORDER  BY LastName, FirstName;

-- Q2. Find users whose email is missing (NULL or empty string).
SELECT * FROM dbo.USERS WHERE Email IS NULL OR LTRIM(RTRIM(Email)) = '';

-- Q3. Why does WHERE Email = NULL return zero rows? Show the right way.
--    Answer: NULL is "unknown", never equal to anything. Use IS NULL.

-- Q4. List users whose first name starts with 'J' (case-insensitive on default collation).
SELECT * FROM dbo.USERS WHERE FirstName LIKE 'J%';

-- Q5. List users whose first name contains exactly one 'a' (any position).
SELECT * FROM dbo.USERS
WHERE  LEN(FirstName) - LEN(REPLACE(LOWER(FirstName), 'a', '')) = 1;

-- Q6. Force a case-SENSITIVE compare regardless of default collation.
SELECT * FROM dbo.USERS WHERE FirstName COLLATE Latin1_General_CS_AS = 'john';

-- Q7. Return the top 5 users by UserId, deterministic ordering.
SELECT TOP (5) WITH TIES * FROM dbo.USERS ORDER BY UserId;

-- Q8. Skip 10, take 5 (server-side paging).
SELECT *
FROM   dbo.USERS
ORDER  BY UserId
OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY;

-- Q9. Categorize users by domain using CASE.
SELECT Email,
       Domain = CASE
                  WHEN Email LIKE '%@gmail.com'    THEN 'Personal'
                  WHEN Email LIKE '%@outlook.com'  THEN 'Personal'
                  WHEN Email LIKE '%@microsoft.com' THEN 'Internal'
                  ELSE 'Other'
                END
FROM   dbo.USERS;

-- Q10. NULL-safe equality: return users where MiddleName matches @m, including both NULL.
DECLARE @m NVARCHAR(50) = NULL;
SELECT * FROM dbo.USERS
WHERE  ISNULL(FirstName, '') = ISNULL(@m, '');     -- one technique
-- or:  EXISTS (SELECT FirstName INTERSECT SELECT @m)

-- Q11. COALESCE vs ISNULL - what's the difference?
--    * COALESCE: ANSI, accepts N args, returns the highest-precedence type.
--    * ISNULL : SQL Server-specific, 2 args, returns the type of the FIRST arg
--      (can silently truncate).

-- Q12. Convert 'YYYY-MM-DD' string to DATE safely.
SELECT TRY_CONVERT(DATE, '2026-02-30') AS bad,    -- NULL (invalid date)
       TRY_CONVERT(DATE, '2026-02-28') AS good;

-- Q13. Distinct vs GROUP BY - same plan?
--    Usually yes; GROUP BY is more flexible (supports HAVING + multiple aggs).

-- Q14. Concatenate FirstName + ' ' + LastName, NULL-safe.
SELECT CONCAT(FirstName, ' ', LastName) AS FullName FROM dbo.USERS;
-- CONCAT treats NULL as empty string; '+' propagates NULL.

-- Q15. STRING_AGG: build a single comma-separated list of all active emails.
SELECT STRING_AGG(Email, ', ') WITHIN GROUP (ORDER BY Email) AS AllEmails
FROM   dbo.USERS WHERE IsActive = 1;


-- =============================================================================
-- B. Aggregates, GROUP BY, HAVING  (Q16-Q30)
-- =============================================================================

-- Q16. Count users per active flag.
SELECT IsActive, COUNT(*) AS Cnt FROM dbo.USERS GROUP BY IsActive;

-- Q17. COUNT(*) vs COUNT(col) vs COUNT(DISTINCT col).
--    COUNT(*)        - all rows including NULLs.
--    COUNT(col)      - non-NULL values of col.
--    COUNT(DISTINCT) - distinct non-NULL values.

-- Q18. Roles that have MORE THAN ONE user assigned.
SELECT RoleId, COUNT(*) AS UserCount
FROM   dbo.USER_ROLES
GROUP  BY RoleId
HAVING COUNT(*) > 1;

-- Q19. Total sales by region, only regions over 10,000.
SELECT Region, SUM(SalesAmount) AS Total
FROM   dbo.SalesData
GROUP  BY Region
HAVING SUM(SalesAmount) > 10000;

-- Q20. WHERE vs HAVING - when do you use each?
--    WHERE filters ROWS before grouping; HAVING filters GROUPS after aggregation.

-- Q21. Average sale per region with a fallback when zero rows.
SELECT Region, ISNULL(AVG(SalesAmount), 0) AS AvgSale
FROM   dbo.SalesData GROUP BY Region;

-- Q22. Department/year totals + subtotals + grand total in ONE query.
WITH d(Department, [Year], Amount) AS (
    SELECT 'Sales', 2026, 100 UNION ALL SELECT 'IT', 2026, 200
    UNION ALL SELECT 'Sales', 2025, 50 UNION ALL SELECT 'IT', 2025, 80)
SELECT Department, [Year], SUM(Amount) AS Total,
       GROUPING_ID(Department, [Year]) AS gid
FROM   d
GROUP BY ROLLUP (Department, [Year])
ORDER BY GROUPING_ID(Department, [Year]), Department, [Year];

-- Q23. Percentage of total per region (window function).
SELECT Region, SUM(SalesAmount) AS Total,
       100.0 * SUM(SalesAmount) / SUM(SUM(SalesAmount)) OVER () AS PctOfTotal
FROM   dbo.SalesData
GROUP  BY Region;

-- Q24. Top 3 highest-paid users per department (assume Salary column on USERS for demo).
--      Use ROW_NUMBER + CTE pattern (see Q63).

-- Q25. Median salary per region (PERCENTILE_CONT).
SELECT DISTINCT Region,
       PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY SalesAmount) OVER (PARTITION BY Region) AS Median
FROM   dbo.SalesData;

-- Q26. Count NULL emails without scanning twice.
SELECT SUM(CASE WHEN Email IS NULL THEN 1 ELSE 0 END) AS NullEmails,
       COUNT(*) AS TotalRows
FROM   dbo.USERS;

-- Q27. Pivot-style: count active vs inactive in single row.
SELECT SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount,
       SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveCount
FROM   dbo.USERS;

-- Q28. Use GROUPING SETS to get totals by region and by salesperson + grand total.
SELECT Region, SalesPerson, SUM(SalesAmount) AS Total
FROM   dbo.SalesData
GROUP  BY GROUPING SETS ( (Region), (SalesPerson), () );

-- Q29. Find duplicate emails (data quality check).
SELECT Email, COUNT(*) AS Dupes
FROM   dbo.USERS
GROUP  BY Email
HAVING COUNT(*) > 1;

-- Q30. Top-paying customer per region (one row per region).
;WITH ranked AS (
    SELECT Region, SalesPerson, SalesAmount,
           ROW_NUMBER() OVER (PARTITION BY Region ORDER BY SalesAmount DESC) AS rn
    FROM   dbo.SalesData)
SELECT Region, SalesPerson, SalesAmount
FROM   ranked WHERE rn = 1;


-- =============================================================================
-- C. Joins (Q31-Q45)
-- =============================================================================

-- Q31. INNER JOIN: list each user with their role name.
SELECT u.UserId, u.FirstName, r.RoleName
FROM   dbo.USERS u
JOIN   dbo.USER_ROLES ur ON ur.UserId = u.UserId
JOIN   dbo.ROLES r       ON r.RoleId  = ur.RoleId;

-- Q32. LEFT JOIN: list ALL users, NULL for those without a role.
SELECT u.UserId, u.FirstName, r.RoleName
FROM   dbo.USERS u
LEFT JOIN dbo.USER_ROLES ur ON ur.UserId = u.UserId
LEFT JOIN dbo.ROLES r       ON r.RoleId  = ur.RoleId;

-- Q33. Find users that have NO role (anti-join 3 ways).
-- 3a NOT EXISTS (preferred - NULL safe + usually best plan):
SELECT * FROM dbo.USERS u
WHERE  NOT EXISTS (SELECT 1 FROM dbo.USER_ROLES ur WHERE ur.UserId = u.UserId);
-- 3b LEFT JOIN ... IS NULL:
SELECT u.* FROM dbo.USERS u
LEFT JOIN dbo.USER_ROLES ur ON ur.UserId = u.UserId
WHERE  ur.UserId IS NULL;
-- 3c NOT IN  (BEWARE: returns NO rows if any subquery row is NULL):
SELECT * FROM dbo.USERS WHERE UserId NOT IN (SELECT UserId FROM dbo.USER_ROLES);

-- Q34. Why prefer NOT EXISTS over NOT IN?
--   NOT IN returns empty result if the inner column has ANY NULLs because
--   "x = NULL" is UNKNOWN. NOT EXISTS handles NULLs correctly.

-- Q35. FULL OUTER JOIN: show everyone, role or no role, and roles with no users.
SELECT u.UserId, u.FirstName, r.RoleId, r.RoleName
FROM   dbo.USERS u
FULL OUTER JOIN dbo.USER_ROLES ur ON ur.UserId = u.UserId
FULL OUTER JOIN dbo.ROLES      r  ON r.RoleId  = ur.RoleId;

-- Q36. SELF JOIN: find pairs of users sharing the same last name.
SELECT a.UserId AS U1, b.UserId AS U2, a.LastName
FROM   dbo.USERS a
JOIN   dbo.USERS b ON b.LastName = a.LastName AND b.UserId > a.UserId;

-- Q37. CROSS JOIN: every user x every role (cartesian).
SELECT u.UserId, r.RoleId FROM dbo.USERS u CROSS JOIN dbo.ROLES r;

-- Q38. CROSS APPLY: top 2 most-recently assigned roles per user.
SELECT u.UserId, x.RoleId
FROM   dbo.USERS u
CROSS APPLY (SELECT TOP (2) RoleId FROM dbo.USER_ROLES ur
             WHERE ur.UserId = u.UserId ORDER BY RoleId DESC) x;

-- Q39. OUTER APPLY: same, but keep users with no roles.
SELECT u.UserId, x.RoleId
FROM   dbo.USERS u
OUTER APPLY (SELECT TOP (2) RoleId FROM dbo.USER_ROLES ur
             WHERE ur.UserId = u.UserId ORDER BY RoleId DESC) x;

-- Q40. Semi-join: users that have at least one permission via roles.
SELECT u.* FROM dbo.USERS u
WHERE EXISTS (
    SELECT 1
    FROM   dbo.USER_ROLES ur
    JOIN   dbo.ROLE_PERMISSION_MAPPING rpm ON rpm.RoleId = ur.RoleId
    WHERE  ur.UserId = u.UserId);

-- Q41. Users that have ALL permissions in the list ('READ','WRITE').
;WITH need(PermName) AS (SELECT 'READ' UNION ALL SELECT 'WRITE')
SELECT u.UserId
FROM   dbo.USERS u
WHERE  NOT EXISTS (
    SELECT 1 FROM need n
    WHERE NOT EXISTS (
        SELECT 1
        FROM   dbo.USER_ROLES ur
        JOIN   dbo.ROLE_PERMISSION_MAPPING rpm ON rpm.RoleId = ur.RoleId
        JOIN   dbo.ROLE_PERMISSIONS p          ON p.PermissionId = rpm.PermissionId
        WHERE  ur.UserId = u.UserId AND p.PermissionName = n.PermName));

-- Q42. Filter on the OUTER vs ON the JOIN - difference?
--   For LEFT JOIN: predicate in ON keeps outer row but nulls the right side;
--   predicate in WHERE turns the LEFT JOIN into an INNER JOIN.

-- Q43. Join hint: force a HASH JOIN (used when optimizer picks wrong).
SELECT u.UserId, r.RoleName
FROM   dbo.USERS u
INNER HASH JOIN dbo.USER_ROLES ur ON ur.UserId = u.UserId
JOIN   dbo.ROLES r ON r.RoleId = ur.RoleId;

-- Q44. EXISTS vs IN - which to use?
--   * IN with small static list / constants -> equivalent.
--   * EXISTS with correlated subquery -> usually better, NULL-safe.

-- Q45. Find users that share ALL roles with user 1.
;WITH refRoles AS (SELECT RoleId FROM dbo.USER_ROLES WHERE UserId = 1)
SELECT ur.UserId
FROM   dbo.USER_ROLES ur
WHERE  ur.UserId <> 1
GROUP  BY ur.UserId
HAVING COUNT(DISTINCT CASE WHEN ur.RoleId IN (SELECT RoleId FROM refRoles) THEN ur.RoleId END)
     = (SELECT COUNT(*) FROM refRoles);


-- =============================================================================
-- D. Subqueries / CTE / Recursion  (Q46-Q60)
-- =============================================================================

-- Q46. Scalar subquery: each user with the count of their roles.
SELECT u.UserId, u.FirstName,
       (SELECT COUNT(*) FROM dbo.USER_ROLES ur WHERE ur.UserId = u.UserId) AS RoleCount
FROM   dbo.USERS u;

-- Q47. Correlated subquery: users whose UserId equals their role count (silly demo).
SELECT u.UserId FROM dbo.USERS u
WHERE  u.UserId = (SELECT COUNT(*) FROM dbo.USER_ROLES ur WHERE ur.UserId = u.UserId);

-- Q48. Derived table: top 5 regions then join back.
SELECT t.*, s.SalesPerson
FROM   (SELECT TOP (5) Region, SUM(SalesAmount) AS Total
        FROM dbo.SalesData GROUP BY Region ORDER BY Total DESC) t
JOIN   dbo.SalesData s ON s.Region = t.Region;

-- Q49. CTE for readability: same as Q48.
;WITH topR AS (
    SELECT TOP (5) Region, SUM(SalesAmount) AS Total
    FROM dbo.SalesData GROUP BY Region ORDER BY Total DESC)
SELECT s.*, topR.Total
FROM   topR JOIN dbo.SalesData s ON s.Region = topR.Region;

-- Q50. Recursive CTE: numbers 1..10.
;WITH n AS (SELECT 1 AS i UNION ALL SELECT i + 1 FROM n WHERE i < 10)
SELECT * FROM n;

-- Q51. Recursive CTE: build a date series (every day in May 2026).
;WITH d AS (
    SELECT CAST('2026-05-01' AS DATE) AS dt
    UNION ALL SELECT DATEADD(DAY, 1, dt) FROM d WHERE dt < '2026-05-31')
SELECT * FROM d OPTION (MAXRECURSION 100);

-- Q52. Employee hierarchy: list each employee with their full management chain.
IF OBJECT_ID('tempdb..#Emp','U') IS NOT NULL DROP TABLE #Emp;
CREATE TABLE #Emp (EmpId INT PRIMARY KEY, Name SYSNAME, MgrId INT NULL);
INSERT INTO #Emp VALUES (1,'CEO',NULL),(2,'VP',1),(3,'Dir',2),(4,'IC',3),(5,'IC2',3);

;WITH chain AS (
    SELECT EmpId, Name, MgrId, 0 AS depth, CAST(Name AS NVARCHAR(MAX)) AS path
    FROM   #Emp WHERE MgrId IS NULL
    UNION ALL
    SELECT e.EmpId, e.Name, e.MgrId, c.depth + 1,
           c.path + N' > ' + e.Name
    FROM   #Emp e JOIN chain c ON c.EmpId = e.MgrId)
SELECT * FROM chain ORDER BY path;

-- Q53. Recursive CTE: split a CSV string into rows (pre-STRING_SPLIT versions).
;WITH split AS (
    SELECT CAST('a,b,c,d' AS NVARCHAR(MAX)) AS s, 1 AS pos
    UNION ALL SELECT s, pos + 1 FROM split WHERE pos < LEN(s))
SELECT DISTINCT value
FROM   STRING_SPLIT('a,b,c,d', ',');     -- modern way

-- Q54. Multi-CTE: stage data, then summarize, then rank.
;WITH cleaned AS (SELECT * FROM dbo.SalesData WHERE SalesAmount > 0),
      agg     AS (SELECT Region, SUM(SalesAmount) AS T FROM cleaned GROUP BY Region),
      ranked  AS (SELECT *, RANK() OVER (ORDER BY T DESC) AS rk FROM agg)
SELECT * FROM ranked WHERE rk <= 3;

-- Q55. CTE vs Subquery vs Temp table - when to use which?
--    CTE       : readability + recursion + single-statement scope.
--    Subquery  : inline, simple.
--    #temp     : reused multiple times, large, stats needed, indexable.

-- Q56. ANY/ALL: regions whose total > ALL other region totals (the max).
;WITH t AS (SELECT Region, SUM(SalesAmount) AS T FROM dbo.SalesData GROUP BY Region)
SELECT * FROM t WHERE T >= ALL (SELECT T FROM t);

-- Q57. EXISTS vs COUNT(*) > 0 - which is faster?
--    EXISTS stops at the first match; COUNT(*) > 0 must count all.

-- Q58. Find gaps in an IDENTITY column.
SELECT (UserId + 1) AS gap_start
FROM   dbo.USERS u
WHERE  NOT EXISTS (SELECT 1 FROM dbo.USERS u2 WHERE u2.UserId = u.UserId + 1)
  AND  u.UserId < (SELECT MAX(UserId) FROM dbo.USERS);

-- Q59. Find islands of consecutive IDs (gaps & islands).
;WITH groups AS (
    SELECT UserId,
           UserId - ROW_NUMBER() OVER (ORDER BY UserId) AS grp
    FROM   dbo.USERS)
SELECT MIN(UserId) AS island_start, MAX(UserId) AS island_end, COUNT(*) AS len
FROM   groups GROUP BY grp ORDER BY island_start;

-- Q60. Pagination with deterministic ORDER BY (NEVER skip ORDER BY!).
SELECT * FROM dbo.USERS
ORDER  BY LastName, FirstName, UserId    -- tiebreaker = unique key
OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY;


-- =============================================================================
-- E. Window functions  (Q61-Q80)
-- =============================================================================

-- Q61. ROW_NUMBER vs RANK vs DENSE_RANK on a small set.
SELECT  v.x,
        ROW_NUMBER() OVER (ORDER BY v.x) AS rn,
        RANK()       OVER (ORDER BY v.x) AS rk,
        DENSE_RANK() OVER (ORDER BY v.x) AS drk
FROM   (VALUES (10),(10),(20),(30),(30),(40)) v(x);

-- Q62. NTILE: split users into 4 quartiles by UserId.
SELECT UserId, NTILE(4) OVER (ORDER BY UserId) AS bucket FROM dbo.USERS;

-- Q63. Top N per group (top 2 highest sales per region).
;WITH r AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY Region ORDER BY SalesAmount DESC) AS rn
    FROM   dbo.SalesData)
SELECT * FROM r WHERE rn <= 2;

-- Q64. Remove duplicates keeping the lowest UserId per Email.
;WITH d AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY Email ORDER BY UserId) AS rn
    FROM   dbo.USERS)
-- DELETE FROM d WHERE rn > 1;
SELECT * FROM d WHERE rn > 1;

-- Q65. Running total of sales by date.
SELECT SaleDate, SalesAmount,
       SUM(SalesAmount) OVER (ORDER BY SaleDate
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningTotal
FROM   dbo.SalesData;

-- Q66. 7-day moving average.
SELECT SaleDate, SalesAmount,
       AVG(SalesAmount * 1.0) OVER (ORDER BY SaleDate
            ROWS BETWEEN 6 PRECEDING AND CURRENT ROW) AS Avg7Day
FROM   dbo.SalesData;

-- Q67. LAG / LEAD: difference vs previous day.
SELECT SaleDate, SalesAmount,
       SalesAmount - LAG(SalesAmount, 1, 0) OVER (ORDER BY SaleDate) AS DeltaPrev
FROM   dbo.SalesData;

-- Q68. FIRST_VALUE / LAST_VALUE per region (note LAST_VALUE frame gotcha).
SELECT Region, SaleDate, SalesAmount,
       FIRST_VALUE(SalesAmount) OVER (PARTITION BY Region ORDER BY SaleDate) AS FirstSale,
       LAST_VALUE (SalesAmount) OVER (PARTITION BY Region ORDER BY SaleDate
            ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) AS LastSale
FROM   dbo.SalesData;

-- Q69. PERCENT_RANK + CUME_DIST.
SELECT SalesAmount,
       PERCENT_RANK() OVER (ORDER BY SalesAmount) AS pct_rank,
       CUME_DIST()    OVER (ORDER BY SalesAmount) AS cume_dist
FROM   dbo.SalesData;

-- Q70. Aggregate window WITHOUT GROUP BY - keep detail rows.
SELECT SaleDate, Region, SalesAmount,
       SUM(SalesAmount) OVER (PARTITION BY Region) AS RegionTotal,
       SalesAmount * 1.0 / SUM(SalesAmount) OVER (PARTITION BY Region) AS ShareOfRegion
FROM   dbo.SalesData;

-- Q71. Detect duplicates (ROW_NUMBER > 1 within natural key).
;WITH d AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY Email ORDER BY UserId) AS rn
    FROM   dbo.USERS)
SELECT * FROM d WHERE rn > 1;

-- Q72. Sessionize events: new session whenever gap > 30 min.
IF OBJECT_ID('tempdb..#Events','U') IS NOT NULL DROP TABLE #Events;
CREATE TABLE #Events(UserId INT, ts DATETIME2);
INSERT INTO #Events VALUES (1,'2026-05-01 09:00'),(1,'2026-05-01 09:10'),
                           (1,'2026-05-01 10:00'),(1,'2026-05-01 10:05');
;WITH gaps AS (
    SELECT UserId, ts,
           CASE WHEN DATEDIFF(MINUTE,
                    LAG(ts) OVER (PARTITION BY UserId ORDER BY ts), ts) > 30
                THEN 1 ELSE 0 END AS NewSession
    FROM   #Events)
SELECT UserId, ts,
       SUM(NewSession) OVER (PARTITION BY UserId ORDER BY ts) AS SessionId
FROM   gaps;

-- Q73. PARTITION BY + ORDER BY semantics - what does ORDER BY change?
--   Adds the running/ranking concept; without it, the window is the whole partition.

-- Q74. Cumulative distinct count (running distinct).
--   Tricky! Often solved with self-join or APPLY, NOT a window function.

-- Q75. Compare a value to the group average (deviation from mean).
SELECT Region, SaleDate, SalesAmount,
       SalesAmount - AVG(SalesAmount * 1.0) OVER (PARTITION BY Region) AS DiffFromAvg
FROM   dbo.SalesData;

-- Q76. Median per group (PERCENTILE_CONT 0.5).
SELECT DISTINCT Region,
       PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY SalesAmount) OVER (PARTITION BY Region) AS Median
FROM   dbo.SalesData;

-- Q77. Find rows where value > previous AND > next (local peak).
SELECT *
FROM (SELECT SaleDate, SalesAmount,
             LAG(SalesAmount)  OVER (ORDER BY SaleDate) AS prev,
             LEAD(SalesAmount) OVER (ORDER BY SaleDate) AS nxt
      FROM dbo.SalesData) t
WHERE SalesAmount > prev AND SalesAmount > nxt;

-- Q78. ROWS vs RANGE - what's the difference?
--   ROWS counts physical rows; RANGE groups by ORDER-BY value (ties included).

-- Q79. Why is LAST_VALUE often "wrong" out of the box?
--   Default frame is RANGE UNBOUNDED PRECEDING AND CURRENT ROW, so LAST_VALUE
--   = current row's value. Fix: explicit frame BETWEEN UNBOUNDED PRECEDING
--   AND UNBOUNDED FOLLOWING.

-- Q80. Top-N per group WITH ties (use RANK() instead of ROW_NUMBER()).
;WITH r AS (
    SELECT *, RANK() OVER (PARTITION BY Region ORDER BY SalesAmount DESC) AS rk
    FROM dbo.SalesData)
SELECT * FROM r WHERE rk <= 3;


-- =============================================================================
-- F. PIVOT / UNPIVOT / Conditional aggregation  (Q81-Q90)
-- =============================================================================

-- Q81. Pivot sales by region into columns.
SELECT *
FROM   (SELECT Region, SalesAmount FROM dbo.SalesData) src
PIVOT  (SUM(SalesAmount) FOR Region IN ([North],[South],[East],[West])) p;

-- Q82. Same with conditional aggregation (more flexible than PIVOT).
SELECT SUM(CASE WHEN Region='North' THEN SalesAmount END) AS North,
       SUM(CASE WHEN Region='South' THEN SalesAmount END) AS South,
       SUM(CASE WHEN Region='East'  THEN SalesAmount END) AS East,
       SUM(CASE WHEN Region='West'  THEN SalesAmount END) AS West
FROM   dbo.SalesData;

-- Q83. Dynamic pivot when columns are unknown at write time.
DECLARE @cols NVARCHAR(MAX), @sql NVARCHAR(MAX);
SELECT @cols = STRING_AGG(QUOTENAME(Region), ',')
FROM  (SELECT DISTINCT Region FROM dbo.SalesData) r;
SET @sql = N'SELECT * FROM (SELECT Region, SalesAmount FROM dbo.SalesData) s
             PIVOT (SUM(SalesAmount) FOR Region IN (' + @cols + N')) p';
EXEC sp_executesql @sql;

-- Q84. UNPIVOT: rotate columns back into rows.
IF OBJECT_ID('tempdb..#Wide') IS NOT NULL DROP TABLE #Wide;
CREATE TABLE #Wide (Id INT, Q1 INT, Q2 INT, Q3 INT, Q4 INT);
INSERT INTO #Wide VALUES (1,100,200,300,400);

SELECT Id, Quarter, Amount
FROM   #Wide
UNPIVOT (Amount FOR Quarter IN (Q1, Q2, Q3, Q4)) u;

-- Q85. UNPIVOT with mixed types - use CROSS APPLY VALUES.
SELECT Id, ColName, ColValue
FROM   #Wide
CROSS APPLY (VALUES ('Q1', Q1), ('Q2', Q2), ('Q3', Q3), ('Q4', Q4)) v(ColName, ColValue);

-- Q86. Pivot with multiple aggregates - use multiple SUM(CASE) columns.

-- Q87. Crosstab: counts per (Region x Status).
SELECT Region,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveSales,
       SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS InactiveSales
FROM  (SELECT s.Region, 1 AS IsActive FROM dbo.SalesData s) x
GROUP BY Region;

-- Q88. Convert key/value EAV rows to columns (anti-pattern fix).
--      EXAMPLE: Attributes(EntityId, AttrName, AttrValue) -> wide row.
--      Use conditional aggregation by AttrName.

-- Q89. Pivot dates by month-of-year.
SELECT *
FROM  (SELECT MONTH(SaleDate) AS m, SalesAmount FROM dbo.SalesData) s
PIVOT (SUM(SalesAmount) FOR m IN ([1],[2],[3],[4],[5],[6],[7],[8],[9],[10],[11],[12])) p;

-- Q90. Why is dynamic PIVOT considered the safer pattern for unknown columns?
--   Because static PIVOT requires knowing the column list at compile time.


-- =============================================================================
-- G. Indexes / SARGability / Plans  (Q91-Q100)
-- =============================================================================

-- Q91. What is a "SARGable" predicate? Show non-SARGable vs SARGable.
-- BAD  (non-SARGable): function on indexed column forces a scan
SELECT * FROM dbo.USERS WHERE UPPER(FirstName) = 'JOHN';
-- GOOD:
SELECT * FROM dbo.USERS WHERE FirstName = 'John';

-- Q92. Date range - SARGable form.
SELECT * FROM dbo.SalesData
WHERE  SaleDate >= '2026-05-01' AND SaleDate < '2026-06-01';   -- good
-- BAD: WHERE YEAR(SaleDate)=2026 AND MONTH(SaleDate)=5  (forces scan)

-- Q93. Clustered vs nonclustered index - one-liner difference?
--   Clustered = leaf is the data (one per table); Nonclustered = leaf is key
--   + RID/clustering key.

-- Q94. Composite index: column order matters (left-most prefix rule).
--   Index on (A,B,C) helps WHERE A=? AND B=? but not WHERE B=?

-- Q95. Covering index: include extra columns in the leaf for "covered" reads.
-- CREATE NONCLUSTERED INDEX IX_USERS_LastName ON dbo.USERS(LastName) INCLUDE (FirstName, Email);

-- Q96. Filtered index: index only a subset of rows.
-- CREATE NONCLUSTERED INDEX IX_USERS_Active ON dbo.USERS(LastName) WHERE IsActive = 1;

-- Q97. Find missing indexes.
SELECT TOP (10)
        d.statement, d.equality_columns, d.inequality_columns, d.included_columns,
        gs.user_seeks, gs.avg_total_user_cost * gs.avg_user_impact AS impact
FROM    sys.dm_db_missing_index_group_stats gs
JOIN    sys.dm_db_missing_index_groups   g ON g.index_group_handle = gs.group_handle
JOIN    sys.dm_db_missing_index_details  d ON d.index_handle       = g.index_handle
ORDER  BY impact DESC;

-- Q98. Find unused indexes (consider dropping).
SELECT  OBJECT_NAME(i.object_id) AS table_name, i.name,
        s.user_seeks, s.user_scans, s.user_lookups, s.user_updates
FROM    sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats s
       ON s.object_id = i.object_id AND s.index_id = i.index_id AND s.database_id = DB_ID()
WHERE   i.type_desc <> 'HEAP' AND OBJECTPROPERTY(i.object_id,'IsUserTable') = 1
ORDER  BY s.user_updates DESC;

-- Q99. Index fragmentation - reorganize vs rebuild thresholds.
--   <5%: leave it. 5-30%: REORGANIZE (online). >30%: REBUILD (with ONLINE=ON if possible).

-- Q100. Implicit conversion eats indexes (interview gotcha).
--   N'literal' vs 'literal' on a varchar column may force conversion;
--   passing INT param to NVARCHAR column gives CONVERT_IMPLICIT => no seek.


-- =============================================================================
-- H. Transactions, isolation, locking  (Q101-Q110)
-- =============================================================================

-- Q101. Name the four read phenomena.
--   Dirty read, non-repeatable read, phantom read, lost update.

-- Q102. Map isolation levels to which phenomena they prevent.
--   READ UNCOMMITTED: prevents nothing.
--   READ COMMITTED:   prevents dirty read.
--   REPEATABLE READ:  prevents dirty + non-repeatable.
--   SERIALIZABLE:     prevents all.
--   SNAPSHOT:         row-versioning, no readers block writers; prevents dirty,
--                     non-repeatable, phantoms (with row versioning trade-offs).

-- Q103. Safe transaction skeleton with TRY/CATCH.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;
    -- DML here
    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;

-- Q104. Deadlock victim detection inside CATCH.
-- IF ERROR_NUMBER() = 1205   -- deadlock victim
--    ... retry logic ...

-- Q105. NOLOCK / READUNCOMMITTED - why dangerous?
--   Can read dirty/missing/duplicate rows from page splits. Prefer RCSI/SNAPSHOT.

-- Q106. RCSI vs SNAPSHOT.
--   RCSI: read-committed via row versioning, no shared locks on reads.
--   SNAPSHOT: full statement-level consistent snapshot, must opt-in per session.

-- Q107. SAVEPOINT: partial rollback inside a transaction.
BEGIN TRAN;
   SAVE TRAN sp1;
   -- some work
   ROLLBACK TRAN sp1;     -- undoes only since SAVE
COMMIT;

-- Q108. @@TRANCOUNT semantics in nested transactions.
--   BEGIN TRAN increments; COMMIT decrements; ROLLBACK rolls back to outer 0.
--   Real "nested" transactions don't exist - only savepoints.

-- Q109. Lock hints: UPDLOCK + HOLDLOCK (read-and-then-write atomicity).
-- SELECT * FROM dbo.USERS WITH (UPDLOCK, HOLDLOCK) WHERE UserId = @id;

-- Q110. Show me current locks for a session.
SELECT * FROM sys.dm_tran_locks WHERE request_session_id = @@SPID;


-- =============================================================================
-- I. Procs / Functions / Triggers  (Q111-Q120)
-- =============================================================================

-- Q111. Scalar UDF vs inline TVF (iTVF) - perf?
--   iTVF expands inline like a view (fast).
--   Scalar UDF historically per-row (slow); 2019+ added "scalar UDF inlining".

-- Q112. Why prefer iTVF over multi-statement TVF (MSTVF)?
--   MSTVF returns a table variable with FIXED estimate of 1 row -> bad plans.

-- Q113. Output parameter pattern.
CREATE OR ALTER PROCEDURE dbo.GetUserCount @C INT OUTPUT AS
BEGIN SET @C = (SELECT COUNT(*) FROM dbo.USERS); END
GO

-- Q114. EXEC with named params + capture OUTPUT.
DECLARE @cnt INT;
EXEC dbo.GetUserCount @C = @cnt OUTPUT;
SELECT @cnt AS UserCount;

-- Q115. Return multiple result sets from a proc - just SELECT multiple times.

-- Q116. AFTER vs INSTEAD OF trigger.
--   AFTER fires post-DML; INSTEAD OF replaces the DML (used to make views updatable).

-- Q117. Inside a trigger: how to know what changed?
--   inserted (NEW rows) + deleted (OLD rows) pseudo-tables. UPDATE() / COLUMNS_UPDATED().

-- Q118. Pass a set of rows to a proc (TVP).
--   CREATE TYPE ... AS TABLE; proc param "@x dbo.MyType READONLY".

-- Q119. Stored proc parameter sniffing fix.
--   Add OPTION (RECOMPILE) or use local variables / OPTIMIZE FOR.

-- Q120. WITH ENCRYPTION on procs - is the code recoverable?
--   It's obfuscated, not encrypted. There are tools to reverse it. Don't rely on it.


-- =============================================================================
-- J. Performance tuning, stats, partitioning  (Q121-Q130)
-- =============================================================================

-- Q121. Update stats for one table.
-- UPDATE STATISTICS dbo.USERS WITH FULLSCAN;

-- Q122. Why might a "good" query suddenly run slow?
--   Stale stats, parameter sniffing, plan eviction, new indexes, growth past
--   a threshold, parallelism changes, blocking.

-- Q123. Detect parameter sniffing in plan cache.
--   sys.dm_exec_query_stats + multiple plan_handles for same query_hash.

-- Q124. Query Store - what does it record?
--   Queries, plans, runtime stats over time. Can force a specific plan.

-- Q125. Partition switching - when is it instant?
--   When source/target schema and constraints match exactly + same filegroup.

-- Q126. Columnstore: rowgroup size goal?
--   ~1,048,576 rows per rowgroup (compressed). Smaller = "trim reason" set.

-- Q127. Top CPU consumers (cached plans).
SELECT TOP (10) qs.execution_count, qs.total_worker_time / 1000 AS total_cpu_ms,
                qs.total_logical_reads, t.text
FROM   sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t
ORDER  BY qs.total_worker_time DESC;

-- Q128. Why is `SELECT *` an interview red flag?
--   Disables covering indexes, ships unused columns, breaks views/procs on
--   schema change, hurts memory grants.

-- Q129. tempdb best practices?
--   Multiple equally-sized data files (1 per logical CPU up to 8), trace flag
--   1117/1118 baked in on 2016+. Watch GAM/SGAM contention.

-- Q130. Plan cache pollution from ad-hoc SQL - mitigation?
--   "Optimize for ad hoc workloads" instance option; use parameterized queries.


-- =============================================================================
-- K. Security, RLS, DDM  (Q131-Q140)
-- =============================================================================

-- Q131. Login vs User vs Schema vs Role.
--   See script 16. Login = server, User = DB, Schema = namespace, Role = group.

-- Q132. GRANT vs DENY vs REVOKE.
--   DENY > GRANT; REVOKE removes either.

-- Q133. Ownership chaining - why it lets a proc read a table?
--   Same owner, no permission check on intermediate object.

-- Q134. SQL injection - safe vs unsafe dynamic SQL.
-- BAD : EXEC ('SELECT * FROM USERS WHERE Name=''' + @n + '''');
-- GOOD: sp_executesql N'... WHERE Name = @n', N'@n NVARCHAR(50)', @n = @n;

-- Q135. RLS predicate function essentials.
--   SCHEMABINDING + inline TVF + uses USER_NAME() / SESSION_CONTEXT().

-- Q136. DDM - is it a security feature?
--   Cosmetic only. Users with UNMASK or schema permission still see plaintext.

-- Q137. Difference TDE vs Always Encrypted.
--   TDE encrypts at-rest, transparent to apps.
--   Always Encrypted: client-side, SQL never sees plaintext.

-- Q138. CONNECT vs IMPERSONATE vs EXECUTE AS.
--   CONNECT: open session; IMPERSONATE: switch context manually;
--   EXECUTE AS: declared on object so callers run as that principal.

-- Q139. Find effective permissions of the current user on a table.
SELECT * FROM sys.fn_my_permissions('dbo.USERS', 'OBJECT');

-- Q140. Audit who deleted rows in a table.
--   Options: temporal tables, CDC, AFTER DELETE trigger, SQL Audit (XE-based).


-- =============================================================================
-- L. Tricky / classic puzzles  (Q141-Q160)
-- =============================================================================

-- Q141. Find Nth highest salary (e.g., 2nd highest distinct).
DECLARE @n INT = 2;
SELECT MIN(Salary) AS NthHighest
FROM  (SELECT DISTINCT TOP (@n) SalesAmount AS Salary FROM dbo.SalesData ORDER BY SalesAmount DESC) t;

-- Q142. Same, using DENSE_RANK (cleaner).
;WITH r AS (SELECT SalesAmount AS Salary,
                   DENSE_RANK() OVER (ORDER BY SalesAmount DESC) AS rk
            FROM dbo.SalesData)
SELECT TOP (1) Salary FROM r WHERE rk = 2;

-- Q143. Swap values of two columns in one UPDATE (no temp variable).
-- UPDATE dbo.USERS SET FirstName = LastName, LastName = FirstName;

-- Q144. Delete duplicate rows, keeping one.
;WITH d AS (
   SELECT *, ROW_NUMBER() OVER (PARTITION BY Email ORDER BY UserId) AS rn FROM dbo.USERS)
-- DELETE FROM d WHERE rn > 1;
SELECT * FROM d WHERE rn > 1;

-- Q145. Find employees earning more than their manager.
--   Self-join: SELECT e.* FROM Emp e JOIN Emp m ON m.EmpId = e.MgrId WHERE e.Salary > m.Salary;

-- Q146. Find consecutive days where sales increased 3 days in a row.
;WITH lagged AS (
   SELECT SaleDate, SalesAmount,
          LAG(SalesAmount, 1) OVER (ORDER BY SaleDate) AS p1,
          LAG(SalesAmount, 2) OVER (ORDER BY SaleDate) AS p2
   FROM dbo.SalesData)
SELECT * FROM lagged WHERE SalesAmount > p1 AND p1 > p2;

-- Q147. Calculate week-over-week growth %.
;WITH wk AS (
   SELECT DATEPART(ISO_WEEK, SaleDate) AS wk, SUM(SalesAmount) AS total
   FROM dbo.SalesData GROUP BY DATEPART(ISO_WEEK, SaleDate))
SELECT wk, total,
       100.0 * (total - LAG(total) OVER (ORDER BY wk)) / NULLIF(LAG(total) OVER (ORDER BY wk),0) AS WoWPct
FROM   wk;

-- Q148. Identify employees who joined the SAME month (peer cohort).
--   GROUP BY YEAR(HireDate), MONTH(HireDate) HAVING COUNT(*) > 1.

-- Q149. Find customers who placed orders every month of 2026.
--   Per customer count(DISTINCT MONTH(OrderDate)) = 12 in that year.

-- Q150. Pivot - turn one row per (Region, Month) into a region grid of 12 months.
SELECT Region,
       SUM(CASE WHEN MONTH(SaleDate)=1  THEN SalesAmount END) AS [Jan],
       SUM(CASE WHEN MONTH(SaleDate)=2  THEN SalesAmount END) AS [Feb],
       SUM(CASE WHEN MONTH(SaleDate)=3  THEN SalesAmount END) AS [Mar],
       SUM(CASE WHEN MONTH(SaleDate)=4  THEN SalesAmount END) AS [Apr],
       SUM(CASE WHEN MONTH(SaleDate)=5  THEN SalesAmount END) AS [May],
       SUM(CASE WHEN MONTH(SaleDate)=6  THEN SalesAmount END) AS [Jun],
       SUM(CASE WHEN MONTH(SaleDate)=7  THEN SalesAmount END) AS [Jul],
       SUM(CASE WHEN MONTH(SaleDate)=8  THEN SalesAmount END) AS [Aug],
       SUM(CASE WHEN MONTH(SaleDate)=9  THEN SalesAmount END) AS [Sep],
       SUM(CASE WHEN MONTH(SaleDate)=10 THEN SalesAmount END) AS [Oct],
       SUM(CASE WHEN MONTH(SaleDate)=11 THEN SalesAmount END) AS [Nov],
       SUM(CASE WHEN MONTH(SaleDate)=12 THEN SalesAmount END) AS [Dec]
FROM   dbo.SalesData GROUP BY Region;

-- Q151. Count customers active in any of the last 30 days.
SELECT COUNT(DISTINCT SalesPerson) FROM dbo.SalesData
WHERE  SaleDate >= DATEADD(DAY, -30, CAST(SYSUTCDATETIME() AS DATE));

-- Q152. Find the longest streak of consecutive days a salesperson sold.
;WITH d AS (
   SELECT SalesPerson, SaleDate,
          DATEADD(DAY, -ROW_NUMBER() OVER (PARTITION BY SalesPerson ORDER BY SaleDate),
                  SaleDate) AS grp
   FROM   dbo.SalesData)
SELECT SalesPerson, MIN(SaleDate) AS streak_start, MAX(SaleDate) AS streak_end,
       COUNT(*) AS streak_days
FROM   d
GROUP  BY SalesPerson, grp
ORDER  BY streak_days DESC;

-- Q153. "Bus factor" - who's the only owner of a role?
SELECT RoleId FROM dbo.USER_ROLES
GROUP BY RoleId HAVING COUNT(*) = 1;

-- Q154. Find the second occurrence of each user's login event.
;WITH r AS (
   SELECT *, ROW_NUMBER() OVER (PARTITION BY UserId ORDER BY CreatedDate) AS rn
   FROM   dbo.USERS)
SELECT * FROM r WHERE rn = 2;

-- Q155. Compute retention: % of users from cohort month who are still active next month.
--   Multi-step: cohort definition + activity check + ratio.

-- Q156. Find rows present in table A but not in table B - 3 ways.
--   EXCEPT, LEFT JOIN ... IS NULL, NOT EXISTS.

-- Q157. Find rows where any column differs between two tables of same schema.
--   SELECT * FROM A EXCEPT SELECT * FROM B  -- value-level compare incl. NULLs.

-- Q158. Generate next business day skipping weekends.
SELECT CASE DATEPART(WEEKDAY, DATEADD(DAY, 1, CAST(SYSUTCDATETIME() AS DATE)))
            WHEN 7 THEN DATEADD(DAY, 3, CAST(SYSUTCDATETIME() AS DATE))   -- Sat -> Mon
            WHEN 1 THEN DATEADD(DAY, 2, CAST(SYSUTCDATETIME() AS DATE))   -- Sun -> Mon
            ELSE        DATEADD(DAY, 1, CAST(SYSUTCDATETIME() AS DATE))
       END AS NextBusinessDay;
-- (assumes default DATEFIRST = 7 i.e. Sunday = 1)

-- Q159. Convert minutes (INT) to 'HH:MM' string.
DECLARE @minutes INT = 145;
SELECT RIGHT('0' + CAST(@minutes / 60 AS VARCHAR(2)), 2) + ':' +
       RIGHT('0' + CAST(@minutes % 60 AS VARCHAR(2)), 2) AS HHMM;

-- Q160. Detect overlapping date ranges per resource.
IF OBJECT_ID('tempdb..#Book') IS NOT NULL DROP TABLE #Book;
CREATE TABLE #Book (Id INT, ResId INT, sd DATE, ed DATE);
INSERT INTO #Book VALUES
 (1,1,'2026-05-01','2026-05-05'),
 (2,1,'2026-05-04','2026-05-07'),  -- overlaps with #1
 (3,1,'2026-05-10','2026-05-12');

SELECT a.Id, b.Id, a.ResId, a.sd, a.ed, b.sd, b.ed
FROM   #Book a JOIN #Book b
   ON  a.ResId = b.ResId
   AND a.Id < b.Id
   AND a.sd <= b.ed AND b.sd <= a.ed;     -- classic overlap predicate


/* =============================================================================
   GENERAL INTERVIEW STRATEGY
   1. Restate the problem in your own words.
   2. State your assumptions (NULLs? duplicates? ties? case sensitivity?).
   3. Write the simplest correct query FIRST. Optimize only if asked.
   4. Mention indexes/SARGability/plan implications proactively.
   5. Validate with a tiny sample dataset (CTE of VALUES is your friend).
   6. Edge cases: empty input, NULLs, duplicates, ties, leap years, time zones.

   COMMON TRAPS
   * NOT IN with NULLs returns empty set.
   * WHERE col = NULL never matches; use IS NULL.
   * GETDATE() vs SYSUTCDATETIME() - time zone awareness.
   * BETWEEN is INCLUSIVE on both ends.
   * Implicit conversions defeat index seeks.
   * SELECT * defeats covering indexes.
   * Default LAST_VALUE() frame is misleading.
   * SCOPE_IDENTITY() vs @@IDENTITY (triggers can pollute @@IDENTITY).
   * MERGE has well-known bugs - know the gotchas.
   * Sort the recordset deterministically before paging.
   ============================================================================= */
