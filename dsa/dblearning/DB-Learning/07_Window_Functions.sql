/* =============================================================================
   Script7.sql  -  WINDOW FUNCTIONS in T-SQL (a.k.a. Analytic / OVER() functions)
   -----------------------------------------------------------------------------
   What is a window function?
     A function that performs a calculation across a SET of rows
     ("window") that is RELATED to the current row - WITHOUT collapsing the
     rows the way GROUP BY does.

   Mental model: GROUP BY squashes 10 rows into 1.
                 Window functions keep all 10 rows and add a new column
                 computed from the "window" each row belongs to.

   Anatomy of OVER():
        <function>() OVER (
            [PARTITION BY <cols>]       -- split rows into independent groups (like GROUP BY)
            [ORDER BY    <cols>]        -- order WITHIN each partition (required for rank/running totals)
            [ROWS|RANGE  <frame>]       -- which rows in the partition are visible to this row
        )

   The 3 families (interview classic):
     1) RANKING       : ROW_NUMBER, RANK, DENSE_RANK, NTILE
     2) AGGREGATE     : SUM/AVG/MIN/MAX/COUNT used with OVER()
     3) VALUE / OFFSET: LAG, LEAD, FIRST_VALUE, LAST_VALUE, PERCENT_RANK, CUME_DIST

   Ranking trio cheat sheet (input salaries: 100, 100, 90):
     ROW_NUMBER -> 1, 2, 3   (unique, arbitrary tie-break)
     RANK       -> 1, 1, 3   (ties share, NEXT rank SKIPS)
     DENSE_RANK -> 1, 1, 2   (ties share, NEXT rank does NOT skip)

   Frame defaults (important & easy to miss):
     * If you specify ORDER BY but NOT a frame, the default is
         RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
       which can produce surprising results for running totals with ties.
       Prefer explicit:  ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW.
   ============================================================================= */


-- =============================================================================
-- 0)  SAMPLE TABLE + DATA  (so every example below is self-contained)
-- =============================================================================
IF OBJECT_ID('dbo.EmployeeSales', 'U') IS NOT NULL DROP TABLE dbo.EmployeeSales;

CREATE TABLE dbo.EmployeeSales (
    EmpId       INT          NOT NULL PRIMARY KEY,
    EmpName     NVARCHAR(50) NOT NULL,
    Department  NVARCHAR(30) NOT NULL,
    HireDate    DATE         NOT NULL,
    Salary      DECIMAL(10,2) NOT NULL,
    SaleMonth   DATE         NOT NULL,        -- first of the month
    SalesAmount DECIMAL(10,2) NOT NULL
);

INSERT INTO dbo.EmployeeSales (EmpId, EmpName, Department, HireDate, Salary, SaleMonth, SalesAmount) VALUES
(1, 'Alice',   'Sales',  '2022-01-10',  60000, '2024-01-01', 1200),
(2, 'Bob',     'Sales',  '2021-03-15',  72000, '2024-01-01', 1500),
(3, 'Carol',   'Sales',  '2023-07-01',  55000, '2024-01-01', 1500),   -- tie with Bob on sales
(4, 'David',   'IT',     '2020-11-20',  90000, '2024-01-01',    0),
(5, 'Eva',     'IT',     '2022-05-05',  85000, '2024-01-01',    0),
(6, 'Frank',   'HR',     '2019-08-12',  65000, '2024-01-01',    0),
-- February
(1, 'Alice',   'Sales',  '2022-01-10',  60000, '2024-02-01', 1800),
(2, 'Bob',     'Sales',  '2021-03-15',  72000, '2024-02-01', 1300),
(3, 'Carol',   'Sales',  '2023-07-01',  55000, '2024-02-01', 2000),
(4, 'David',   'IT',     '2020-11-20',  90000, '2024-02-01',    0),
(5, 'Eva',     'IT',     '2022-05-05',  85000, '2024-02-01',    0),
(6, 'Frank',   'HR',     '2019-08-12',  65000, '2024-02-01',    0),
-- March
(1, 'Alice',   'Sales',  '2022-01-10',  60000, '2024-03-01', 2200),
(2, 'Bob',     'Sales',  '2021-03-15',  72000, '2024-03-01', 1700),
(3, 'Carol',   'Sales',  '2023-07-01',  55000, '2024-03-01', 1900),
(4, 'David',   'IT',     '2020-11-20',  90000, '2024-03-01',    0),
(5, 'Eva',     'IT',     '2022-05-05',  85000, '2024-03-01',    0),
(6, 'Frank',   'HR',     '2019-08-12',  65000, '2024-03-01',    0);
GO
-- NOTE: EmpId is repeated across months on purpose, so the PK above will
-- actually fail. For a clean run, drop the PK or use a composite PK.
-- (Keeping the schema simple - if you re-run, recreate without the PK.)


-- Quick fix: recreate without the PK so the inserts work without complaint.
IF OBJECT_ID('dbo.EmployeeSales', 'U') IS NOT NULL DROP TABLE dbo.EmployeeSales;

CREATE TABLE dbo.EmployeeSales (
    EmpId       INT          NOT NULL,
    EmpName     NVARCHAR(50) NOT NULL,
    Department  NVARCHAR(30) NOT NULL,
    HireDate    DATE         NOT NULL,
    Salary      DECIMAL(10,2) NOT NULL,
    SaleMonth   DATE         NOT NULL,
    SalesAmount DECIMAL(10,2) NOT NULL,
    CONSTRAINT PK_EmployeeSales PRIMARY KEY (EmpId, SaleMonth)   -- composite PK: one row per employee per month
);

INSERT INTO dbo.EmployeeSales (EmpId, EmpName, Department, HireDate, Salary, SaleMonth, SalesAmount) VALUES
(1,'Alice','Sales','2022-01-10',60000,'2024-01-01',1200),
(2,'Bob',  'Sales','2021-03-15',72000,'2024-01-01',1500),
(3,'Carol','Sales','2023-07-01',55000,'2024-01-01',1500),
(4,'David','IT',   '2020-11-20',90000,'2024-01-01',   0),
(5,'Eva',  'IT',   '2022-05-05',85000,'2024-01-01',   0),
(6,'Frank','HR',   '2019-08-12',65000,'2024-01-01',   0),
(1,'Alice','Sales','2022-01-10',60000,'2024-02-01',1800),
(2,'Bob',  'Sales','2021-03-15',72000,'2024-02-01',1300),
(3,'Carol','Sales','2023-07-01',55000,'2024-02-01',2000),
(4,'David','IT',   '2020-11-20',90000,'2024-02-01',   0),
(5,'Eva',  'IT',   '2022-05-05',85000,'2024-02-01',   0),
(6,'Frank','HR',   '2019-08-12',65000,'2024-02-01',   0),
(1,'Alice','Sales','2022-01-10',60000,'2024-03-01',2200),
(2,'Bob',  'Sales','2021-03-15',72000,'2024-03-01',1700),
(3,'Carol','Sales','2023-07-01',55000,'2024-03-01',1900),
(4,'David','IT',   '2020-11-20',90000,'2024-03-01',   0),
(5,'Eva',  'IT',   '2022-05-05',85000,'2024-03-01',   0),
(6,'Frank','HR',   '2019-08-12',65000,'2024-03-01',   0);
GO


-- =============================================================================
-- 1) RANKING FUNCTIONS - ROW_NUMBER / RANK / DENSE_RANK
-- =============================================================================
-- Plain English:
--   * PARTITION BY Department -> "restart the numbering for each department".
--   * ORDER BY Salary DESC    -> "highest salary gets rank 1 within its dept".
-- Compare the three columns for Sales (Alice 60k, Bob 72k, Carol 55k):
--   ROW_NUMBER : 1 Bob, 2 Alice, 3 Carol  (unique numbers, arbitrary tie-break)
--   RANK       : ties share, next rank SKIPS the count
--   DENSE_RANK : ties share, next rank does NOT skip
SELECT DISTINCT
       EmpName, Department, Salary,
       ROW_NUMBER() OVER (PARTITION BY Department ORDER BY Salary DESC) AS RowNum,
       RANK()       OVER (PARTITION BY Department ORDER BY Salary DESC) AS Rnk,
       DENSE_RANK() OVER (PARTITION BY Department ORDER BY Salary DESC) AS DenseRnk
FROM dbo.EmployeeSales;


-- Classic interview problem: "Top N per group" - here, top 2 salaried per department.
-- The CTE pattern is the canonical solution.
WITH Ranked AS
(
    SELECT EmpName, Department, Salary,
           ROW_NUMBER() OVER (PARTITION BY Department ORDER BY Salary DESC) AS rn
    FROM (SELECT DISTINCT EmpName, Department, Salary FROM dbo.EmployeeSales) AS u
)
SELECT EmpName, Department, Salary
FROM   Ranked
WHERE  rn <= 2;


-- NTILE(n) - split rows into n roughly-equal buckets (handy for quartiles / deciles).
SELECT DISTINCT
       EmpName, Department, Salary,
       NTILE(4) OVER (ORDER BY Salary DESC) AS SalaryQuartile   -- 1 = highest quartile
FROM dbo.EmployeeSales;


-- =============================================================================
-- 2) AGGREGATES with OVER()  - keep rows, add a partition-wide value
-- =============================================================================
-- Without OVER(): one row per department.    WITH OVER(): every row keeps its
-- detail AND sees the dept total / average alongside it.
SELECT DISTINCT
       EmpName, Department, Salary,
       SUM(Salary) OVER (PARTITION BY Department) AS DeptTotalSalary,
       AVG(Salary) OVER (PARTITION BY Department) AS DeptAvgSalary,
       MIN(Salary) OVER (PARTITION BY Department) AS DeptMinSalary,
       MAX(Salary) OVER (PARTITION BY Department) AS DeptMaxSalary,
       COUNT(*)    OVER (PARTITION BY Department) AS DeptHeadcount,
       Salary * 100.0 / SUM(Salary) OVER (PARTITION BY Department) AS PctOfDeptPayroll
FROM dbo.EmployeeSales;


-- =============================================================================
-- 3) RUNNING TOTALS & MOVING AVERAGES (frame clause)
-- =============================================================================
-- Running total of an employee's sales month over month.
--   ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
--   = "sum from the FIRST row in this partition up to (and including) the current row"
SELECT EmpName, SaleMonth, SalesAmount,
       SUM(SalesAmount) OVER (
            PARTITION BY EmpName
            ORDER BY     SaleMonth
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
       ) AS RunningTotal
FROM dbo.EmployeeSales
WHERE Department = 'Sales'
ORDER BY EmpName, SaleMonth;


-- 3-month moving average (window of current row + 2 preceding).
SELECT EmpName, SaleMonth, SalesAmount,
       AVG(SalesAmount) OVER (
            PARTITION BY EmpName
            ORDER BY     SaleMonth
            ROWS BETWEEN 2 PRECEDING AND CURRENT ROW
       ) AS Moving3MoAvg
FROM dbo.EmployeeSales
WHERE Department = 'Sales'
ORDER BY EmpName, SaleMonth;


-- =============================================================================
-- 4) OFFSET FUNCTIONS  - LAG / LEAD / FIRST_VALUE / LAST_VALUE
-- =============================================================================
-- LAG  = "value from the PREVIOUS row in the partition" (peek BACKWARD).
-- LEAD = "value from the NEXT row in the partition"     (peek FORWARD).
-- Both take an optional offset (default 1) and default value (default NULL).
SELECT EmpName, SaleMonth, SalesAmount,
       LAG (SalesAmount, 1, 0) OVER (PARTITION BY EmpName ORDER BY SaleMonth) AS PrevMonthSales,
       LEAD(SalesAmount, 1, 0) OVER (PARTITION BY EmpName ORDER BY SaleMonth) AS NextMonthSales,
       SalesAmount - LAG(SalesAmount, 1, 0) OVER (PARTITION BY EmpName ORDER BY SaleMonth) AS MoMChange
FROM dbo.EmployeeSales
WHERE Department = 'Sales'
ORDER BY EmpName, SaleMonth;


-- FIRST_VALUE / LAST_VALUE - first/last row of the window.
-- GOTCHA: LAST_VALUE with the default frame returns the CURRENT row, not the
-- partition-last row. To get the true last value, expand the frame explicitly.
SELECT EmpName, SaleMonth, SalesAmount,
       FIRST_VALUE(SalesAmount) OVER (
            PARTITION BY EmpName ORDER BY SaleMonth
       ) AS FirstMonthSales,
       LAST_VALUE(SalesAmount)  OVER (
            PARTITION BY EmpName ORDER BY SaleMonth
            ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING   -- needed for "true last"
       ) AS LastMonthSales
FROM dbo.EmployeeSales
WHERE Department = 'Sales'
ORDER BY EmpName, SaleMonth;


-- =============================================================================
-- 5) DISTRIBUTION FUNCTIONS - PERCENT_RANK / CUME_DIST
-- =============================================================================
-- PERCENT_RANK : (rank - 1) / (n - 1)        -> always 0 for the first row, 1 for the last
-- CUME_DIST    : count of rows <= current / n -> share of rows at or below the current value
SELECT DISTINCT
       EmpName, Department, Salary,
       PERCENT_RANK() OVER (PARTITION BY Department ORDER BY Salary) AS PctRank,
       CUME_DIST()    OVER (PARTITION BY Department ORDER BY Salary) AS CumeDist
FROM dbo.EmployeeSales;


-- =============================================================================
-- 6) NAMED WINDOWS (WINDOW clause)  - DRY when many functions share a window
-- =============================================================================
-- Available from SQL Server 2022+. Define the window ONCE, reference by name.
SELECT EmpName, SaleMonth, SalesAmount,
       SUM(SalesAmount) OVER w AS RunningTotal,
       AVG(SalesAmount) OVER w AS RunningAvg
FROM dbo.EmployeeSales
WHERE Department = 'Sales'
WINDOW w AS (PARTITION BY EmpName ORDER BY SaleMonth
             ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW)
ORDER BY EmpName, SaleMonth;


-- =============================================================================
-- 7) COMMON INTERVIEW PATTERNS
-- =============================================================================

-- a) De-duplicate rows, keeping the "best" one per key.
--    Pattern: ROW_NUMBER over a partition, then keep rn = 1.
WITH dedup AS
(
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY EmpId, SaleMonth ORDER BY SalesAmount DESC) AS rn
    FROM dbo.EmployeeSales
)
SELECT EmpId, EmpName, SaleMonth, SalesAmount
FROM   dedup
WHERE  rn = 1;


-- b) "Gaps and Islands" - find consecutive months an employee made > 0 sales.
--    The classic trick: ROW_NUMBER over the ordered set minus ROW_NUMBER over
--    the filtered set yields a constant "island id" per consecutive run.
WITH s AS
(
    SELECT EmpName, SaleMonth, SalesAmount,
           ROW_NUMBER() OVER (PARTITION BY EmpName ORDER BY SaleMonth) -
           ROW_NUMBER() OVER (PARTITION BY EmpName, CASE WHEN SalesAmount > 0 THEN 1 ELSE 0 END
                              ORDER BY SaleMonth) AS grp
    FROM dbo.EmployeeSales
    WHERE Department = 'Sales'
)
SELECT EmpName,
       MIN(SaleMonth) AS StreakStart,
       MAX(SaleMonth) AS StreakEnd,
       COUNT(*)       AS Months
FROM   s
WHERE  SalesAmount > 0
GROUP BY EmpName, grp
ORDER BY EmpName, StreakStart;


-- c) Nth highest salary per department (e.g. 2nd highest).
WITH r AS
(
    SELECT DISTINCT EmpName, Department, Salary,
           DENSE_RANK() OVER (PARTITION BY Department ORDER BY Salary DESC) AS dr
    FROM dbo.EmployeeSales
)
SELECT EmpName, Department, Salary
FROM   r
WHERE  dr = 2;


/* =============================================================================
   QUICK INTERVIEW REVIEW
   -----------------------------------------------------------------------------
   * GROUP BY collapses; OVER() does not.
   * PARTITION BY is "GROUP BY" for the window. ORDER BY orders WITHIN it.
   * ROW_NUMBER / RANK / DENSE_RANK differ only in how they handle ties.
   * For running totals always specify  ROWS BETWEEN ... AND CURRENT ROW  to
     avoid the RANGE default surprise.
   * LAG / LEAD give "previous" / "next" row values without a self join.
   * LAST_VALUE needs an explicit  ROWS BETWEEN UNBOUNDED PRECEDING AND
     UNBOUNDED FOLLOWING  to mean what you think it means.
   * "Top N per group", "de-duplication" and "gaps & islands" are 90% of
     window-function interview questions - memorize those patterns.
   ============================================================================= */
