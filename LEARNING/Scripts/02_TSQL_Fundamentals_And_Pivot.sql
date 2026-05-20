/* =============================================================================
   Script2.sql  -  T-SQL fundamentals: SELECT, WHERE, LIKE, CASE, GROUP BY,
                   HAVING, date / string / NULL functions, and PIVOT setup.
   -----------------------------------------------------------------------------
   Logical query processing order (interview classic):
       FROM -> WHERE -> GROUP BY -> HAVING -> SELECT -> ORDER BY
   Aliases declared in SELECT are NOT visible in WHERE / GROUP BY / HAVING
   because SELECT runs after them.
   ============================================================================= */


-- -----------------------------------------------------------------------------
-- 1) Basic projection
-- -----------------------------------------------------------------------------
-- SELECT *  -> returns every column. Convenient for exploration, but avoid in
-- production code: it's fragile (schema changes break consumers) and prevents
-- index covering / column-store optimizations.
SELECT * FROM dbo.USERS;
GO

-- Project only the columns we need -> less I/O, narrower result set.
SELECT FirstName, LastName FROM dbo.USERS;
GO


-- -----------------------------------------------------------------------------
-- 2) Filtering with WHERE
-- -----------------------------------------------------------------------------
-- BIT comparison: IsActive = 1 returns only "active" users.
SELECT FirstName, LastName FROM dbo.USERS WHERE IsActive = 1;
GO

-- LIKE with '%' wildcard - '%' matches zero or more characters.
-- '%@example.com' = ends with @example.com.
SELECT FirstName, LastName FROM dbo.USERS WHERE Email LIKE '%@example.com';
GO

-- 'A%' = starts with A (SARGable -> can use an index on FirstName).
SELECT FirstName, LastName FROM dbo.USERS WHERE FirstName LIKE 'A%';
GO

-- '%son' = ends with "son" (NOT SARGable -> forces a scan; common interview gotcha).
SELECT FirstName, LastName FROM dbo.USERS WHERE LastName LIKE '%son';
GO

-- Combining predicates with AND - both conditions must be true.
SELECT FirstName, LastName
FROM dbo.USERS
WHERE Email LIKE '%@example.com' AND IsActive = 1;
GO


-- -----------------------------------------------------------------------------
-- 3) CASE expression - inline IF/ELSE for the SELECT list.
--    CASE is an EXPRESSION (returns a value), not a statement.
-- -----------------------------------------------------------------------------
SELECT FirstName,
       LastName,
       CASE WHEN IsActive = 1 THEN 'ACTIVE' ELSE 'IN-ACTIVE' END AS STATUS
FROM dbo.USERS;


-- -----------------------------------------------------------------------------
-- 4) GROUP BY + aggregate functions (SUM, AVG, COUNT, MIN, MAX)
--    Every non-aggregated column in SELECT MUST appear in GROUP BY.
-- -----------------------------------------------------------------------------
SELECT IsActive, COUNT(*) AS UserCount
FROM dbo.USERS
GROUP BY IsActive;
GO


-- -----------------------------------------------------------------------------
-- 5) HAVING - filter AFTER aggregation.
--    Rule of thumb:   WHERE filters rows, HAVING filters groups.
--    HAVING can reference aggregates (COUNT/SUM/...) which WHERE cannot.
-- -----------------------------------------------------------------------------
SELECT IsActive, COUNT(*) AS UserCount
FROM dbo.USERS
GROUP BY IsActive
HAVING COUNT(*) > 1;
GO


-- -----------------------------------------------------------------------------
-- 6) Date functions
-- -----------------------------------------------------------------------------
-- GETDATE()    -> server local datetime
-- GETUTCDATE() -> current UTC (use this for storage to avoid timezone bugs)
-- DATEADD      -> add an interval to a date (here: +7 days)
-- DATEDIFF     -> difference between two dates in the requested unit
SELECT GETDATE()                                AS CurrentDateTime,
       GETUTCDATE()                             AS CurrentUTCDateTime,
       DATEADD(DAY, 7, GETDATE())               AS DateAfter7Days,
       DATEDIFF(DAY, '2024-01-01', GETDATE())   AS DaysSince2024;
GO

-- Date "parts" extraction:
--   DAY/MONTH/YEAR -> integer parts of a date
--   DATEPART(WEEKDAY, ...) -> integer day-of-week (depends on @@DATEFIRST)
--   DATENAME(WEEKDAY, ...) -> string day name ('Monday' etc., language-dependent)
SELECT GETDATE()                       AS CurrentDateTime,
       DAY(GETDATE())                  AS DayOfMonth,
       MONTH(GETDATE())                AS [Month],
       YEAR(GETDATE())                 AS [Year],
       DATEPART(WEEKDAY, GETDATE())    AS WeekdayNumber,
       DATENAME(WEEKDAY, GETDATE())    AS WeekdayName;

-- Computing an approximate DOB by subtracting 30 years from today.
-- (DATEADD with a negative interval is the idiomatic way to "go back in time".)
SELECT FirstName, LastName,
       DATEADD(YEAR, -30, GETDATE()) AS ApproxDOB
FROM dbo.USERS;
GO


-- -----------------------------------------------------------------------------
-- 7) String functions - using "MAHESH IS WORKING FOR SQL" as the sample input.
--    NOTE: INITCAP and INSTR are Oracle/MySQL functions, NOT T-SQL - they are
--    intentionally commented out below. T-SQL equivalents:
--        INITCAP  -> no direct built-in; emulate with STRING_SPLIT + UPPER/LOWER
--        INSTR    -> CHARINDEX(substring, string)
-- -----------------------------------------------------------------------------
SELECT 'MAHESH IS WORKING FOR SQL'                                              AS OriginalString
     , RIGHT('MAHESH IS WORKING FOR SQL', 3)                                    AS Right3Chars        -- last 3 chars
     , LEFT('MAHESH IS WORKING FOR SQL', 6)                                     AS Left6Chars         -- first 6 chars
     , LEN('MAHESH IS WORKING FOR SQL')                                         AS StringLength       -- LEN ignores trailing spaces; DATALENGTH does not
     , TRIM('   MAHESH IS WORKING FOR SQL   ')                                  AS TrimmedString      -- removes both ends
     , RTRIM('   MAHESH IS WORKING FOR SQL   ')                                 AS RTrimmedString     -- trailing only
     , LTRIM('   MAHESH IS WORKING FOR SQL   ')                                 AS LTrimmedString     -- leading only
     , REPLACE('MAHESH IS WORKING FOR SQL', 'SQL', 'DATABASE')                  AS ReplacedString     -- replace ALL occurrences
     , REVERSE('MAHESH IS WORKING FOR SQL')                                     AS ReversedString
     , SUBSTRING('MAHESH IS WORKING FOR SQL', 1, 6)                             AS SubstringExample   -- 1-based start, length
     , CONCAT('MAHESH', ' ', 'IS', ' ', 'WORKING', ' ', 'FOR', ' ', 'SQL')      AS ConcatenatedString -- CONCAT treats NULL as ''
     , CONCAT_WS(' ', 'MAHESH', 'IS', 'WORKING', 'FOR', 'SQL')                  AS ConcatenatedWSString -- "with separator" - cleaner than CONCAT for delimited lists
     , UPPER('MAHESH IS WORKING FOR SQL')                                       AS UpperString
     , LOWER('MAHESH IS WORKING FOR SQL')                                       AS LowerString;
--   , INITCAP('MAHESH IS WORKING FOR SQL')                                    AS InitCapString   -- not valid in T-SQL
--   , INSTR('MAHESH IS WORKING FOR SQL', 'WORKING')                            AS InstrPosition;  -- use CHARINDEX in T-SQL
GO


-- -----------------------------------------------------------------------------
-- 8) NULL handling - one of the most-asked interview topics.
--    Key facts:
--      * NULL means "unknown", not "empty".
--      * Any comparison with NULL using =, <>, <, > yields UNKNOWN (treated
--        as false in WHERE). That's why you MUST use IS NULL / IS NOT NULL,
--        never  = NULL.
--      * COALESCE(a, b, c) returns the first non-NULL argument (ANSI standard,
--        accepts N args, returns the highest-precedence data type).
--      * ISNULL(a, b)  is T-SQL specific, only 2 args, and the return type
--        matches the FIRST argument (can cause silent truncation).
-- -----------------------------------------------------------------------------
SELECT FirstName, LastName, Email, Phone,
       COALESCE(Phone, 'No Phone') AS PhoneWithDefault
FROM dbo.USERS;
GO

SELECT FirstName, LastName, Email, Phone,
       ISNULL(Phone, 'No Phone') AS PhoneWithDefault
FROM dbo.USERS;
GO

-- Correct way to find rows where Phone is unknown.
SELECT FirstName, LastName, Email, Phone
FROM dbo.USERS
WHERE Phone IS NULL;



-- -----------------------------------------------------------------------------
-- 9) PIVOT / UNPIVOT setup
--    PIVOT  : rotate ROWS  into COLUMNS (transpose) - one row per Product,
--             one column per Year.
--    UNPIVOT: opposite - turn COLUMNS back into ROWS.
-- -----------------------------------------------------------------------------
CREATE TABLE SalesData (
    Product     NVARCHAR(50),
    Year        INT,
    SalesAmount DECIMAL(10, 2)              -- DECIMAL(precision, scale) - exact numeric, safe for money
);
GO

INSERT INTO SalesData (Product, Year, SalesAmount) VALUES
('Product A', 2022, 1000.00),
('Product A', 2023, 1500.00),
('Product B', 2022, 2000.00),
('Product B', 2023, 2500.00);
GO


-- -----------------------------------------------------------------------------
-- TRANSPOSE: rows -> columns using PIVOT
-- -----------------------------------------------------------------------------
-- Why your earlier snippet didn't work:
--   It only had a derived table aliased as SourceTable and was missing the
--   actual  PIVOT ( <agg> FOR <col> IN (<values>) ) AS <alias>  clause.
--   A derived table alone cannot transpose anything.
--
-- Anatomy of a PIVOT query (interview-ready breakdown):
--   1. SOURCE  : a derived table that exposes ONLY three things -
--                  a) the grouping key      (Product)        -> stays as a row
--                  b) the spreading column  (Year)           -> becomes new columns
--                  c) the value to aggregate (SalesAmount)   -> fills the cells
--                Any extra column would silently create extra groups, so keep it minimal.
--   2. PIVOT   : SUM(SalesAmount) FOR [Year] IN ([2022], [2023])
--                  - The aggregate (SUM here) is REQUIRED, even if only one row is expected.
--                  - The IN (...) list must be hard-coded; use dynamic SQL for an unknown list.
--   3. ALIAS   : the PIVOT result MUST be aliased (here "p").
-- Result shape: one row per Product, one column per Year.
SELECT Product,
       [2022] AS Sales2022,
       [2023] AS Sales2023
FROM (
        SELECT Product, [Year], SalesAmount    -- only the 3 needed columns
        FROM SalesData
     ) AS SourceTable
PIVOT (
        SUM(SalesAmount)                       -- aggregate that fills each cell
        FOR [Year] IN ([2022], [2023])         -- spreading column + value list
     ) AS p;
GO


-- -----------------------------------------------------------------------------
-- UNPIVOT: columns -> rows  (the reverse transpose)
-- -----------------------------------------------------------------------------
-- Takes the wide PIVOT result and folds the Year columns back into rows.
SELECT Product, [Year], SalesAmount
FROM (
        SELECT Product, [2022], [2023]
        FROM (
                SELECT Product, [Year], SalesAmount
                FROM SalesData
             ) src
        PIVOT (SUM(SalesAmount) FOR [Year] IN ([2022], [2023])) AS pvt
     ) AS Wide
UNPIVOT (
        SalesAmount FOR [Year] IN ([2022], [2023])
     ) AS u;
GO


-- -----------------------------------------------------------------------------
-- DYNAMIC PIVOT - when the list of years (or any spreading values) is not
-- known at compile time. Builds the IN(...) list at runtime from the data.
-- Interview note: with dynamic SQL always use QUOTENAME / parameters to
-- avoid SQL injection when any input is user-supplied.
-- -----------------------------------------------------------------------------
DECLARE @cols  NVARCHAR(MAX),
        @query NVARCHAR(MAX);

-- Build a comma-separated list like "[2022],[2023]" from the distinct Years.
SELECT @cols = STRING_AGG(QUOTENAME([Year]), ',')
FROM (SELECT DISTINCT [Year] FROM SalesData) AS y;

SET @query = N'
    SELECT Product, ' + @cols + N'
    FROM (
            SELECT Product, [Year], SalesAmount
            FROM SalesData
         ) AS SourceTable
    PIVOT (
            SUM(SalesAmount) FOR [Year] IN (' + @cols + N')
         ) AS p;';

EXEC sp_executesql @query;
GO


-- -----------------------------------------------------------------------------
-- Alternative transpose WITHOUT PIVOT - conditional aggregation.
-- Works on every SQL dialect, often easier to read, and tends to optimize well.
-- -----------------------------------------------------------------------------
SELECT Product,
       SUM(CASE WHEN [Year] = 2022 THEN SalesAmount END) AS Sales2022,
       SUM(CASE WHEN [Year] = 2023 THEN SalesAmount END) AS Sales2023
FROM SalesData
GROUP BY Product;
GO

