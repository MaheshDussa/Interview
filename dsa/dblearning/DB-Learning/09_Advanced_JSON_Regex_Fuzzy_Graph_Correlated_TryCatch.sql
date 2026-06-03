/* =============================================================================
   Script9.sql  -  Advanced T-SQL toolkit (interview-oriented)
   -----------------------------------------------------------------------------
   Topics covered (each section is self-contained and runnable):
       1) Process JSON data with built-in functions
       2) Match patterns with regular expressions (LIKE / PATINDEX
          + SQL Server 2025 REGEXP_* family)
       3) Find approximate matches with fuzzy string functions
          (SOUNDEX, DIFFERENCE, edit distance helper)
       4) Traverse relationships with graph queries (NODE / EDGE / MATCH /
          SHORTEST_PATH)
       5) Compare rows with correlated subqueries
       6) Handle errors with TRY ... CATCH (+ THROW / XACT_STATE / XACT_ABORT)
       7) Exercise - work with JSON functions

   These mirror the Microsoft Learn modules of the same names. Comments are
   intentionally plain English and call out the points interviewers probe.
   ============================================================================= */


-- =============================================================================
-- 1) PROCESS JSON DATA WITH BUILT-IN FUNCTIONS
-- =============================================================================
-- Plain English: SQL Server treats JSON as NVARCHAR text. There is no JSON
-- data type. The engine ships with functions to read, validate, shred and
-- build JSON without leaving T-SQL.
--
-- The 5 essential JSON functions:
--   ISJSON(x)               -> 1 if x is valid JSON text, else 0
--   JSON_VALUE(x, '$.path') -> SCALAR value (string up to 4000 chars)
--   JSON_QUERY(x, '$.path') -> JSON FRAGMENT (object or array, NOT scalar)
--   OPENJSON(x [, '$.path'])-> shred JSON into a rowset (great with WITH clause)
--   FOR JSON PATH / AUTO    -> turn a SELECT into a JSON string
--
-- Interview gotchas:
--   * JSON_VALUE returns NULL for objects/arrays; JSON_QUERY returns NULL for scalars.
--   * Paths are case-sensitive and 1-based for arrays: '$.tags[0]'.
--   * 'lax' (default) returns NULL on missing path; 'strict' throws an error.
-- -----------------------------------------------------------------------------

DECLARE @json NVARCHAR(MAX) = N'{
    "orderId": 1001,
    "customer": { "id": 7, "name": "Alice" },
    "items": [
        { "sku": "A-100", "qty": 2, "price": 19.95 },
        { "sku": "B-200", "qty": 1, "price": 49.00 }
    ],
    "tags": [ "priority", "gift" ]
}';

-- Validate.
SELECT ISJSON(@json) AS IsValidJson;

-- Read scalar values.
SELECT JSON_VALUE(@json, '$.orderId')        AS OrderId,
       JSON_VALUE(@json, '$.customer.name')  AS CustomerName,
       JSON_VALUE(@json, '$.tags[0]')        AS FirstTag;

-- Read JSON fragments (objects/arrays) - use JSON_QUERY, NOT JSON_VALUE.
SELECT JSON_QUERY(@json, '$.customer') AS CustomerObj,
       JSON_QUERY(@json, '$.items')    AS ItemsArray;

-- Shred an array into a rowset with OPENJSON + WITH (typed columns).
SELECT *
FROM OPENJSON(@json, '$.items')
WITH (
    Sku   NVARCHAR(20) '$.sku',
    Qty   INT          '$.qty',
    Price DECIMAL(10,2) '$.price'
);

-- Build JSON from a SELECT.
SELECT TOP (3) UserId, FirstName, LastName, Email
FROM   dbo.USERS
FOR JSON PATH, ROOT('users');
GO


-- =============================================================================
-- 2) MATCH PATTERNS WITH REGULAR EXPRESSIONS
-- =============================================================================
-- Classic T-SQL pattern matching (works on ALL versions):
--   * LIKE wildcards:  %   _   [abc]   [^abc]   [a-z]
--   * PATINDEX        : returns the 1-based position of the FIRST match (or 0)
--   * CHARINDEX       : exact substring search (no patterns)
--
-- SQL Server 2025 (Azure SQL DB / SQL 2025 preview) ships native regex:
--   REGEXP_LIKE / REGEXP_COUNT / REGEXP_INSTR / REGEXP_REPLACE / REGEXP_SUBSTR
-- Earlier versions need a CLR function or pre-process in the app tier.
--
-- Interview talking points:
--   * Leading '%' kills index usage (non-SARGable). Trailing '%' is fine.
--   * Inside [] you can use ranges and negation: '[A-Z]', '[^0-9]'.
--   * Escape wildcard characters with the ESCAPE clause: LIKE '50!%' ESCAPE '!'.
-- -----------------------------------------------------------------------------

DECLARE @sample TABLE (s NVARCHAR(100));
INSERT INTO @sample VALUES
(N'alice@example.com'),
(N'bob.smith@contoso.org'),
(N'not-an-email'),
(N'support+tag@example.com'),
(N'1234567890');

-- LIKE pattern: looks like an email (very loose).
SELECT s
FROM   @sample
WHERE  s LIKE '%_@_%._%';

-- LIKE with character classes: starts with a letter, then word chars.
SELECT s
FROM   @sample
WHERE  s LIKE '[A-Za-z]%';

-- PATINDEX: position of the first digit run (returns 0 if none).
SELECT s,
       PATINDEX('%[0-9]%', s) AS FirstDigitPos
FROM   @sample;

-- True regex (SQL Server 2025 +). Comment out on older versions.
-- SELECT s
-- FROM   @sample
-- WHERE  REGEXP_LIKE(s, '^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$');
GO


-- =============================================================================
-- 3) FUZZY STRING MATCHING
-- =============================================================================
-- Built-ins:
--   SOUNDEX(s)        -> 4-char phonetic code (e.g. 'Smith' = 'S530')
--   DIFFERENCE(a, b)  -> similarity of two SOUNDEX codes, 0 (low) .. 4 (high)
--
-- For real similarity scoring, SOUNDEX is too coarse. Two better options:
--   * SQL Server with the Master Data Services / Fuzzy Lookup transform (SSIS).
--   * A T-SQL Levenshtein (edit-distance) function - shown below as an iTVF-like
--     scalar function. Distance 0 = identical; small numbers = close matches.
--
-- Interview talking points:
--   * SOUNDEX is English-centric and ignores vowels - it's a quick first cut
--     for "does this NAME roughly sound the same?" matching.
--   * For multi-language / typo tolerance, use Levenshtein, Jaro-Winkler, or
--     trigram similarity (commonly added via CLR / external code).
-- -----------------------------------------------------------------------------

SELECT SOUNDEX('Smith')      AS S_Smith,
       SOUNDEX('Smyth')      AS S_Smyth,
       SOUNDEX('Schmidt')    AS S_Schmidt,
       DIFFERENCE('Smith', 'Smyth')   AS Diff_Smith_Smyth,    -- 4 (very similar)
       DIFFERENCE('Smith', 'Schmidt') AS Diff_Smith_Schmidt;  -- 2 (loose match)

-- Find users whose first name sounds like 'Eric' (catches Erik, Erick, Erica...).
SELECT FirstName, LastName
FROM   dbo.USERS
WHERE  DIFFERENCE(FirstName, 'Eric') >= 3;

-- Simple Levenshtein scalar function (interview-friendly: small + readable).
-- Note: scalar UDFs in WHERE on big tables are slow; here it's for demo only.
CREATE OR ALTER FUNCTION dbo.fn_Levenshtein
(
    @a NVARCHAR(100),
    @b NVARCHAR(100)
)
RETURNS INT
WITH SCHEMABINDING
AS
BEGIN
    IF @a IS NULL OR @b IS NULL RETURN NULL;

    DECLARE @la INT = LEN(@a), @lb INT = LEN(@b);
    IF @la = 0 RETURN @lb;
    IF @lb = 0 RETURN @la;

    -- Two rolling rows of the DP matrix.
    DECLARE @prev TABLE (i INT PRIMARY KEY, v INT);
    DECLARE @curr TABLE (i INT PRIMARY KEY, v INT);

    DECLARE @i INT = 0;
    WHILE @i <= @lb
    BEGIN
        INSERT @prev VALUES (@i, @i);
        SET @i += 1;
    END;

    SET @i = 1;
    WHILE @i <= @la
    BEGIN
        DELETE @curr;
        INSERT @curr VALUES (0, @i);

        DECLARE @j INT = 1;
        WHILE @j <= @lb
        BEGIN
            DECLARE @cost INT =
                CASE WHEN SUBSTRING(@a, @i, 1) = SUBSTRING(@b, @j, 1) THEN 0 ELSE 1 END;

            DECLARE @ins INT = (SELECT v FROM @curr WHERE i = @j - 1) + 1;
            DECLARE @del INT = (SELECT v FROM @prev WHERE i = @j    ) + 1;
            DECLARE @sub INT = (SELECT v FROM @prev WHERE i = @j - 1) + @cost;

            INSERT @curr VALUES (@j, (SELECT MIN(x) FROM (VALUES (@ins),(@del),(@sub)) AS v(x)));
            SET @j += 1;
        END;

        DELETE @prev;
        INSERT @prev SELECT i, v FROM @curr;
        SET @i += 1;
    END;

    RETURN (SELECT v FROM @prev WHERE i = @lb);
END
GO

SELECT dbo.fn_Levenshtein('kitten', 'sitting') AS Distance;   -- 3
GO


-- =============================================================================
-- 4) TRAVERSE RELATIONSHIPS WITH GRAPH QUERIES
-- =============================================================================
-- See Script8 for the deep dive. Recap with a fresh tiny example:
--   NODE  table -> entities.    EDGE  table -> relationships.
--   MATCH(a-(e)->b) walks edges. SHORTEST_PATH does variable-length traversal.
-- -----------------------------------------------------------------------------

IF OBJECT_ID('dbo.Knows', 'U') IS NOT NULL DROP TABLE dbo.Knows;
IF OBJECT_ID('dbo.Member','U') IS NOT NULL DROP TABLE dbo.Member;

CREATE TABLE dbo.Member (
    MemberId INT NOT NULL PRIMARY KEY,
    Name     NVARCHAR(50) NOT NULL
) AS NODE;

CREATE TABLE dbo.Knows (
    SinceUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT EC_Knows CONNECTION (dbo.Member TO dbo.Member)
) AS EDGE;
GO

INSERT INTO dbo.Member (MemberId, Name) VALUES (1,'Ann'),(2,'Ben'),(3,'Cara'),(4,'Dan');
INSERT INTO dbo.Knows ($from_id, $to_id)
SELECT a.$node_id, b.$node_id
FROM dbo.Member a, dbo.Member b
WHERE (a.Name='Ann' AND b.Name='Ben')
   OR (a.Name='Ben' AND b.Name='Cara')
   OR (a.Name='Cara' AND b.Name='Dan');

-- Who does Ann know directly?
SELECT m2.Name
FROM   dbo.Member m1, dbo.Knows k, dbo.Member m2
WHERE  MATCH(m1-(k)->m2) AND m1.Name = 'Ann';

-- Shortest path from Ann to anyone reachable.
SELECT  STRING_AGG(reached.Name, ' -> ') WITHIN GROUP (GRAPH PATH) AS Path
FROM    dbo.Member AS start,
        dbo.Knows  FOR PATH AS k,
        dbo.Member FOR PATH AS reached
WHERE   MATCH(SHORTEST_PATH(start(-(k)->reached)+))
    AND start.Name = 'Ann';
GO


-- =============================================================================
-- 5) COMPARE ROWS WITH CORRELATED SUBQUERIES
-- =============================================================================
-- A correlated subquery REFERENCES a column of the outer query. Conceptually
-- it runs once per outer row; the optimizer often rewrites it as a join.
-- Use it for "row vs aggregate of its group" or "exists / not exists" checks.
--
-- Three classic patterns:
--   a) Filter:   WHERE col > (SELECT AVG(col) FROM ... WHERE group = outer.group)
--   b) Existence: WHERE EXISTS (SELECT 1 FROM child WHERE child.fk = outer.pk)
--   c) Lookup column in SELECT list (single value only).
--
-- Interview talking points:
--   * EXISTS / NOT EXISTS short-circuit and are usually cheaper than COUNT(*) > 0.
--   * NOT IN behaves badly with NULLs (returns NULL/unknown -> rows dropped).
--     Prefer NOT EXISTS for anti-join semantics.
--   * Window functions can often replace correlated subqueries with one pass.
-- -----------------------------------------------------------------------------

-- Users whose UserId is ABOVE the average UserId (toy example).
SELECT UserId, FirstName, LastName
FROM   dbo.USERS u
WHERE  u.UserId > (SELECT AVG(UserId) FROM dbo.USERS);

-- Users WHO HAVE AT LEAST ONE role (EXISTS = semi-join).
SELECT u.UserId, u.FirstName
FROM   dbo.USERS u
WHERE  EXISTS (SELECT 1 FROM dbo.USER_ROLES ur WHERE ur.UserId = u.UserId);

-- Users WITHOUT any role (NOT EXISTS = anti-join, NULL-safe).
SELECT u.UserId, u.FirstName
FROM   dbo.USERS u
WHERE  NOT EXISTS (SELECT 1 FROM dbo.USER_ROLES ur WHERE ur.UserId = u.UserId);

-- Per-row lookup: number of roles each user has (single-value scalar subquery).
SELECT u.UserId, u.FirstName,
       (SELECT COUNT(*) FROM dbo.USER_ROLES ur WHERE ur.UserId = u.UserId) AS RoleCount
FROM   dbo.USERS u;
GO


-- =============================================================================
-- 6) HANDLE ERRORS WITH TRY ... CATCH
-- =============================================================================
-- T-SQL has structured exception handling like other languages:
--      BEGIN TRY ... END TRY  BEGIN CATCH ... END CATCH
--
-- Error info available inside CATCH (do NOT rely on @@ERROR after multiple stmts):
--      ERROR_NUMBER(), ERROR_MESSAGE(), ERROR_SEVERITY(),
--      ERROR_STATE(),  ERROR_LINE(),    ERROR_PROCEDURE()
--
-- Best practices interviewers expect:
--   * SET XACT_ABORT ON  -> any runtime error rolls back the whole batch.
--   * Wrap DML in BEGIN TRAN ... COMMIT inside TRY; ROLLBACK inside CATCH
--     only when XACT_STATE() <> 0.
--   * Raise errors with THROW (preserves number/severity) - RAISERROR is legacy.
--   * User-defined error numbers must be >= 50000.
--   * Some errors are NOT catchable (severity >= 20, compile errors in the
--     same batch, KILL, etc.).
-- -----------------------------------------------------------------------------

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRAN;

    -- Force a primary-key violation to exercise the CATCH block.
    INSERT INTO dbo.ROLES (RoleId, RoleName) VALUES (1, 'DuplicateRole');

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRAN;

    SELECT
        ERROR_NUMBER()    AS ErrNum,
        ERROR_SEVERITY()  AS ErrSeverity,
        ERROR_STATE()     AS ErrState,
        ERROR_LINE()      AS ErrLine,
        ERROR_PROCEDURE() AS ErrProc,
        ERROR_MESSAGE()   AS ErrMessage;

    -- Re-raise to the caller while keeping the original error info.
    THROW;
END CATCH;
GO

-- Raising a custom error explicitly with THROW.
-- Signature: THROW <error_number>, <message>, <state>;
BEGIN TRY
    THROW 50050, 'Custom business rule failed: balance cannot be negative.', 1;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() AS ErrNum, ERROR_MESSAGE() AS ErrMessage;
END CATCH;
GO


-- =============================================================================
-- 7) EXERCISE - WORK WITH JSON FUNCTIONS
-- =============================================================================
-- A small end-to-end exercise tying it all together. Read it like a kata.
--
-- Scenario:
--   We receive raw orders as JSON in a staging column. Build a query that:
--     1. Validates the JSON.
--     2. Flattens header + items into relational columns.
--     3. Computes the total amount per order.
--     4. Returns the result as JSON again for the downstream API.
-- -----------------------------------------------------------------------------

IF OBJECT_ID('dbo.OrderStaging', 'U') IS NOT NULL DROP TABLE dbo.OrderStaging;
CREATE TABLE dbo.OrderStaging
(
    StagingId INT IDENTITY(1,1) PRIMARY KEY,
    RawJson   NVARCHAR(MAX) NOT NULL
);

INSERT INTO dbo.OrderStaging (RawJson) VALUES
(N'{
    "orderId": 5001,
    "customer": "Alice",
    "items": [
        {"sku":"A-1","qty":2,"price":10.00},
        {"sku":"B-2","qty":1,"price":25.00}
    ]
}'),
(N'{
    "orderId": 5002,
    "customer": "Bob",
    "items": [
        {"sku":"C-3","qty":4,"price":7.50}
    ]
}'),
(N'this is not json');                              -- intentional bad row

-- Step 1 + 2 + 3: validate, shred, aggregate.
WITH parsed AS
(
    SELECT s.StagingId,
           JSON_VALUE(s.RawJson, '$.orderId')   AS OrderId,
           JSON_VALUE(s.RawJson, '$.customer')  AS CustomerName,
           i.Sku, i.Qty, i.Price
    FROM   dbo.OrderStaging s
    CROSS APPLY OPENJSON(s.RawJson, '$.items')
                WITH (Sku   NVARCHAR(20) '$.sku',
                      Qty   INT          '$.qty',
                      Price DECIMAL(10,2) '$.price') i
    WHERE  ISJSON(s.RawJson) = 1                    -- skip the bad row
),
totals AS
(
    SELECT OrderId, CustomerName,
           SUM(Qty * Price) AS OrderTotal,
           -- Build the items array per order as a JSON fragment.
           (SELECT Sku, Qty, Price
            FROM   parsed p2
            WHERE  p2.OrderId = p1.OrderId
            FOR JSON PATH) AS ItemsJson
    FROM   parsed p1
    GROUP BY OrderId, CustomerName
)
-- Step 4: emit a single JSON document for the API.
SELECT OrderId, CustomerName, OrderTotal,
       JSON_QUERY(ItemsJson) AS Items                -- JSON_QUERY so the array is embedded, not escaped
FROM   totals
ORDER BY OrderId
FOR JSON PATH, ROOT('orders');
GO


/* =============================================================================
   QUICK INTERVIEW REVIEW
   -----------------------------------------------------------------------------
   * JSON         : ISJSON / JSON_VALUE (scalar) / JSON_QUERY (object|array) /
                    OPENJSON (shred) / FOR JSON (build).
   * Patterns     : LIKE wildcards + character classes; PATINDEX for position;
                    SQL 2025 REGEXP_* family for true regex.
   * Fuzzy        : SOUNDEX + DIFFERENCE for "sounds-like"; Levenshtein for
                    typo tolerance; prefer Jaro-Winkler / trigrams at scale.
   * Graph        : NODE / EDGE tables; MATCH(a-(e)->b); SHORTEST_PATH for
                    variable-length traversal.
   * Correlated   : EXISTS / NOT EXISTS over IN / NOT IN (NULL-safe); window
                    functions often replace correlated subqueries with one pass.
   * Errors       : SET XACT_ABORT ON + TRY/CATCH + THROW; check XACT_STATE()
                    before ROLLBACK; use ERROR_* functions inside CATCH.
   ============================================================================= */
