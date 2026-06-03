/* =============================================================================
   02_Tricky_Temp_Table_Scenarios.sql
   -----------------------------------------------------------------------------
   100+ TRICKY T-SQL INTERVIEW QUESTIONS built around a tiny #TEMP1 dataset.

   Theme of the seed problem:
       SELECT * FROM #TEMP1 ORDER BY CASE WHEN ID = 2 THEN 0 ELSE 1 END, ID;
       --> "Pin a specific row to the top, then natural order."

   Each section pushes that idea further: custom ordering, NULL handling,
   gaps & islands, dedupe, pivots, recursion, JSON, dynamic SQL, window
   functions, set operations, MERGE, OUTPUT, isolation, etc.

   How to use:
     1. Run the SETUP block once per session.
     2. Each "Q###" below is a self-contained problem. The expected technique
        / answer is shown immediately under it. Try to solve before peeking.
   ============================================================================= */

SET NOCOUNT ON;
GO

/* -----------------------------------------------------------------------------
   SETUP: base #TEMP1 + a couple of helper tables used later
   ----------------------------------------------------------------------------- */
IF OBJECT_ID('tempdb..#TEMP1')    IS NOT NULL DROP TABLE #TEMP1;
IF OBJECT_ID('tempdb..#TEMP2')    IS NOT NULL DROP TABLE #TEMP2;
IF OBJECT_ID('tempdb..#Sales')    IS NOT NULL DROP TABLE #Sales;
IF OBJECT_ID('tempdb..#Emp')      IS NOT NULL DROP TABLE #Emp;
GO

CREATE TABLE #TEMP1
(
    ID      INT IDENTITY(1,1) PRIMARY KEY,
    Product NVARCHAR(255) NOT NULL UNIQUE
);

INSERT INTO #TEMP1 (Product) VALUES
('Product A'),('Product B'),('Product C'),
('Product D'),('Product E'),('Product F');

-- A second table for JOIN / set-op tricks
CREATE TABLE #TEMP2
(
    ID      INT PRIMARY KEY,
    Product NVARCHAR(255) NULL,
    Price   DECIMAL(10,2) NULL
);
INSERT INTO #TEMP2 VALUES
(2,'Product B',  20.00),
(3,'Product C',  30.00),
(4,'Product D',  NULL),
(5,'Product E',  50.00),
(7,'Product G',  70.00),
(8, NULL,        80.00);

-- A small sales fact table
CREATE TABLE #Sales
(
    SaleID    INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT      NOT NULL,
    SaleDate  DATE     NOT NULL,
    Qty       INT      NOT NULL,
    Amount    DECIMAL(10,2) NOT NULL
);
INSERT INTO #Sales (ProductID,SaleDate,Qty,Amount) VALUES
(1,'2026-01-01',2, 20),(1,'2026-01-02',1, 10),(1,'2026-02-15',5, 60),
(2,'2026-01-03',3, 60),(2,'2026-03-04',1, 25),
(3,'2026-02-01',4, 80),(3,'2026-02-02',4, 80),(3,'2026-02-03',4, 80),
(4,'2026-01-10',1,  9),(4,'2026-01-11',1,  9),
(5,'2026-04-01',10,500),
(6,'2026-04-02',0,  0);

-- A small employee/manager table for recursion
CREATE TABLE #Emp
(
    EmpID    INT PRIMARY KEY,
    EmpName  NVARCHAR(50),
    ManagerID INT NULL,
    Salary   DECIMAL(10,2)
);
INSERT INTO #Emp VALUES
(1,'CEO',   NULL,500000),
(2,'VP-A',  1,   300000),
(3,'VP-B',  1,   310000),
(4,'Mgr-A1',2,   180000),
(5,'Mgr-A2',2,   175000),
(6,'Mgr-B1',3,   190000),
(7,'Eng-1', 4,    90000),
(8,'Eng-2', 4,    92000),
(9,'Eng-3', 5,    88000),
(10,'Eng-4',6,    95000);
GO


/* =============================================================================
   SECTION 1 — CUSTOM ORDERING & "PIN THE ROW" TRICKS  (Q1 - Q12)
   ============================================================================= */

-- Q1. Pin ID=4 to the top, then natural order.
SELECT * FROM #TEMP1
ORDER BY CASE WHEN ID = 4 THEN 0 ELSE 1 END, ID;

-- Q2. Pin ID=2 to the top AND ID=5 to the bottom; others natural order.
SELECT * FROM #TEMP1
ORDER BY CASE WHEN ID=2 THEN 0 WHEN ID=5 THEN 2 ELSE 1 END, ID;

-- Q3. Order by an explicit priority list: 3,1,5, then everything else by ID.
SELECT * FROM #TEMP1
ORDER BY CASE ID WHEN 3 THEN 1 WHEN 1 THEN 2 WHEN 5 THEN 3 ELSE 4 END, ID;

-- Q4. Preserve the order of a CSV IN-list ('Product D','Product A','Product C').
SELECT *
FROM   #TEMP1
WHERE  Product IN ('Product D','Product A','Product C')
ORDER BY CHARINDEX(',' + Product + ',',
                   ',Product D,Product A,Product C,');

-- Q5. Move all "even ID" rows to the top, sorted DESC; odd IDs after, ASC.
SELECT *
FROM   #TEMP1
ORDER BY CASE WHEN ID % 2 = 0 THEN 0 ELSE 1 END,
         CASE WHEN ID % 2 = 0 THEN -ID ELSE ID END;

-- Q6. Sort alphabetically but case-insensitively forced (regardless of collation).
SELECT * FROM #TEMP1
ORDER BY Product COLLATE Latin1_General_CI_AI;

-- Q7. Order so NULLs come LAST (SQL Server sorts NULLs first by default on ASC).
SELECT * FROM #TEMP2
ORDER BY CASE WHEN Product IS NULL THEN 1 ELSE 0 END, Product;

-- Q8. Order so NULLs come FIRST on DESC (reverse of default).
SELECT * FROM #TEMP2
ORDER BY CASE WHEN Price IS NULL THEN 0 ELSE 1 END, Price DESC;

-- Q9. Random shuffle (deterministic per-row via NEWID()).
SELECT * FROM #TEMP1 ORDER BY NEWID();

-- Q10. "Stable" pseudo-random ordering keyed by a seed column (hash the ID).
SELECT * FROM #TEMP1
ORDER BY HASHBYTES('SHA1', CAST(ID AS VARBINARY(4)));

-- Q11. Top 3 in custom priority order without TOP (use OFFSET/FETCH).
SELECT * FROM #TEMP1
ORDER BY CASE WHEN ID IN (4,2,6) THEN 0 ELSE 1 END, ID
OFFSET 0 ROWS FETCH NEXT 3 ROWS ONLY;

-- Q12. Alternate rows: odd IDs ascending, even IDs interleaved descending.
SELECT *
FROM (
    SELECT t.*,
           ROW_NUMBER() OVER (PARTITION BY ID%2 ORDER BY
               CASE WHEN ID%2=0 THEN -ID ELSE ID END) rn
    FROM #TEMP1 t
) x
ORDER BY rn, ID%2;


/* =============================================================================
   SECTION 2 — NULLs, EMPTY STRINGS, AND THREE-VALUED LOGIC  (Q13 - Q22)
   ============================================================================= */

-- Q13. Why does WHERE Product <> NULL return zero rows? Fix it.
SELECT * FROM #TEMP2 WHERE Product IS NOT NULL;     -- correct
-- SELECT * FROM #TEMP2 WHERE Product <> NULL;      -- wrong (UNKNOWN)

-- Q14. Treat NULL Product as 'UNKNOWN' and sort.
SELECT ID, ISNULL(Product,'UNKNOWN') AS Product, Price
FROM   #TEMP2 ORDER BY 2;

-- Q15. Count rows where Price is NULL vs NOT NULL in ONE query.
SELECT SUM(CASE WHEN Price IS NULL     THEN 1 ELSE 0 END) AS NullPrices,
       SUM(CASE WHEN Price IS NOT NULL THEN 1 ELSE 0 END) AS NonNullPrices
FROM #TEMP2;

-- Q16. NULL-safe equality join between #TEMP1 and #TEMP2 on Product
--      (NULL must match NULL).
SELECT t1.ID, t1.Product, t2.Price
FROM #TEMP1 t1
FULL JOIN #TEMP2 t2
  ON EXISTS (SELECT t1.Product INTERSECT SELECT t2.Product);

-- Q17. Difference between COUNT(*), COUNT(1), COUNT(Product). Show all 3.
SELECT COUNT(*) AS CntStar, COUNT(1) AS CntOne, COUNT(Product) AS CntProduct
FROM   #TEMP2;

-- Q18. Average Price ignoring NULLs vs treating NULLs as 0.
SELECT AVG(Price)             AS AvgIgnoringNulls,
       AVG(ISNULL(Price,0))   AS AvgTreatingNullAsZero
FROM   #TEMP2;

-- Q19. Find products that exist in #TEMP1 but NOT in #TEMP2 — including NULLs.
SELECT Product FROM #TEMP1
EXCEPT
SELECT Product FROM #TEMP2;     -- EXCEPT treats NULL = NULL

-- Q20. Same as above but using NOT EXISTS (watch the NULL pitfall vs NOT IN).
SELECT t1.Product
FROM   #TEMP1 t1
WHERE  NOT EXISTS (SELECT 1 FROM #TEMP2 t2 WHERE t2.Product = t1.Product);

-- Q21. Why does NOT IN go wrong when the subquery has a NULL? Demonstrate.
-- (Returns ZERO rows because of UNKNOWN propagation)
SELECT Product FROM #TEMP1
WHERE  Product NOT IN (SELECT Product FROM #TEMP2);

-- Q22. Coalesce vs ISNULL: which one preserves higher-precision data type?
SELECT
    ISNULL  (CAST(NULL AS VARCHAR(3)), 'ABCDE') AS UsingISNULL,   -- 'ABC'
    COALESCE(CAST(NULL AS VARCHAR(3)), 'ABCDE') AS UsingCOALESCE; -- 'ABCDE'


/* =============================================================================
   SECTION 3 — DEDUPLICATION & "FIND THE Nth"  (Q23 - Q32)
   ============================================================================= */

-- Q23. Insert duplicates into a staging table and dedupe keeping lowest ID.
IF OBJECT_ID('tempdb..#Dup') IS NOT NULL DROP TABLE #Dup;
SELECT * INTO #Dup FROM #TEMP1;
INSERT INTO #Dup(Product) VALUES('Product A'),('Product A'),('Product C');

;WITH x AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY Product ORDER BY ID) rn
    FROM #Dup
)
DELETE FROM x WHERE rn > 1;
SELECT * FROM #Dup;

-- Q24. Find the 2nd highest ProductID that has any sales.
SELECT TOP 1 ProductID
FROM (
    SELECT DISTINCT ProductID
    FROM #Sales
    ORDER BY ProductID DESC
    OFFSET 1 ROWS FETCH NEXT 1 ROW ONLY
) x;

-- Q25. Nth highest using DENSE_RANK (ties share a rank).
DECLARE @N INT = 2;
SELECT ProductID, TotalAmt
FROM (
    SELECT ProductID, SUM(Amount) TotalAmt,
           DENSE_RANK() OVER (ORDER BY SUM(Amount) DESC) dr
    FROM #Sales GROUP BY ProductID
) x WHERE dr = @N;

-- Q26. Top 2 sales PER product (per-group Top-N).
SELECT *
FROM (
    SELECT s.*,
           ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY Amount DESC) rn
    FROM #Sales s
) x WHERE rn <= 2;

-- Q27. Median Amount per product (PERCENTILE_CONT).
SELECT DISTINCT ProductID,
       PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY Amount)
            OVER (PARTITION BY ProductID) AS MedianAmt
FROM #Sales;

-- Q28. Mode (most frequent Amount) per product.
SELECT ProductID, Amount
FROM (
    SELECT ProductID, Amount,
           RANK() OVER (PARTITION BY ProductID ORDER BY COUNT(*) DESC) rk
    FROM #Sales GROUP BY ProductID, Amount
) x WHERE rk = 1;

-- Q29. Detect exact-duplicate rows ignoring SaleID.
SELECT ProductID, SaleDate, Qty, Amount, COUNT(*) AS dupes
FROM   #Sales
GROUP  BY ProductID, SaleDate, Qty, Amount
HAVING COUNT(*) > 1;

-- Q30. Delete duplicate sales (same ProductID/SaleDate/Qty/Amount) keeping MIN(SaleID).
;WITH d AS (
    SELECT *, ROW_NUMBER() OVER
        (PARTITION BY ProductID,SaleDate,Qty,Amount ORDER BY SaleID) rn
    FROM #Sales)
DELETE FROM d WHERE rn > 1;

-- Q31. Products with NO sales at all (anti-join).
SELECT t.*
FROM   #TEMP1 t
WHERE  NOT EXISTS (SELECT 1 FROM #Sales s WHERE s.ProductID = t.ID);

-- Q32. Products whose EVERY sale has Qty > 0 (relational division "for all").
SELECT t.*
FROM   #TEMP1 t
WHERE  EXISTS (SELECT 1 FROM #Sales s WHERE s.ProductID=t.ID)
  AND  NOT EXISTS (SELECT 1 FROM #Sales s
                   WHERE s.ProductID=t.ID AND s.Qty <= 0);


/* =============================================================================
   SECTION 4 — GAPS & ISLANDS, RUNNING TOTALS, WINDOWS  (Q33 - Q47)
   ============================================================================= */

-- Q33. Find gaps in the ID sequence of #TEMP1 after deleting ID=3.
DELETE FROM #TEMP1 WHERE ID = 3;
SELECT prev_id = ID,
       next_id = LEAD(ID) OVER (ORDER BY ID),
       gap_start = ID + 1,
       gap_end   = LEAD(ID) OVER (ORDER BY ID) - 1
FROM   #TEMP1
WHERE  LEAD(ID) OVER (ORDER BY ID) - ID > 1;
-- restore for downstream queries
INSERT INTO #TEMP1(Product) VALUES('Product C-restored');

-- Q34. Islands of consecutive IDs.
;WITH s AS (
    SELECT ID, ID - ROW_NUMBER() OVER (ORDER BY ID) grp FROM #TEMP1
)
SELECT MIN(ID) island_start, MAX(ID) island_end, COUNT(*) sz
FROM s GROUP BY grp ORDER BY island_start;

-- Q35. Running total of Amount per ProductID ordered by SaleDate.
SELECT *,
       SUM(Amount) OVER (PARTITION BY ProductID ORDER BY SaleDate
                         ROWS UNBOUNDED PRECEDING) AS RunningTot
FROM   #Sales;

-- Q36. 3-row moving average of Amount per product.
SELECT *,
       AVG(Amount*1.0) OVER (PARTITION BY ProductID ORDER BY SaleDate
                             ROWS BETWEEN 2 PRECEDING AND CURRENT ROW) AS MA3
FROM   #Sales;

-- Q37. Difference from previous sale (LAG) and next sale (LEAD) per product.
SELECT *,
       Amount - LAG(Amount)  OVER (PARTITION BY ProductID ORDER BY SaleDate) AS DiffPrev,
       LEAD(Amount) OVER (PARTITION BY ProductID ORDER BY SaleDate) - Amount AS DiffNext
FROM   #Sales;

-- Q38. % of total sales per product (window SUM without GROUP BY).
SELECT ProductID,
       SUM(Amount) AS Total,
       100.0*SUM(Amount)/SUM(SUM(Amount)) OVER () AS PctOfGrand
FROM   #Sales GROUP BY ProductID;

-- Q39. First and last sale per product in one row (FIRST_VALUE / LAST_VALUE).
SELECT DISTINCT ProductID,
       FIRST_VALUE(Amount) OVER (PARTITION BY ProductID ORDER BY SaleDate
                                 ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) FirstAmt,
       LAST_VALUE (Amount) OVER (PARTITION BY ProductID ORDER BY SaleDate
                                 ROWS BETWEEN UNBOUNDED PRECEDING AND UNBOUNDED FOLLOWING) LastAmt
FROM #Sales;

-- Q40. RANK vs DENSE_RANK vs ROW_NUMBER on Amount per product.
SELECT *,
       ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY Amount DESC) rn,
       RANK()       OVER (PARTITION BY ProductID ORDER BY Amount DESC) rk,
       DENSE_RANK() OVER (PARTITION BY ProductID ORDER BY Amount DESC) dr
FROM #Sales;

-- Q41. NTILE: split products into 3 sales buckets.
SELECT ProductID, Total,
       NTILE(3) OVER (ORDER BY Total DESC) Bucket
FROM (SELECT ProductID, SUM(Amount) Total FROM #Sales GROUP BY ProductID) x;

-- Q42. Cumulative distribution of Amount.
SELECT Amount,
       CUME_DIST()    OVER (ORDER BY Amount) cd,
       PERCENT_RANK() OVER (ORDER BY Amount) pr
FROM #Sales;

-- Q43. Detect first sale per product (rn=1 trick).
SELECT * FROM (
    SELECT s.*, ROW_NUMBER() OVER (PARTITION BY ProductID ORDER BY SaleDate) rn
    FROM #Sales s) x WHERE rn = 1;

-- Q44. Sessionize: group sales into "sessions" where gap > 7 days.
;WITH x AS (
    SELECT *,
           CASE WHEN DATEDIFF(DAY,
                    LAG(SaleDate) OVER (PARTITION BY ProductID ORDER BY SaleDate),
                    SaleDate) > 7
                OR LAG(SaleDate) OVER (PARTITION BY ProductID ORDER BY SaleDate) IS NULL
                THEN 1 ELSE 0 END AS NewSess
    FROM #Sales),
y AS (
    SELECT *, SUM(NewSess) OVER (PARTITION BY ProductID ORDER BY SaleDate
                                 ROWS UNBOUNDED PRECEDING) SessId
    FROM x)
SELECT ProductID, SessId, MIN(SaleDate) StartD, MAX(SaleDate) EndD, SUM(Amount) Tot
FROM y GROUP BY ProductID, SessId;

-- Q45. Find the longest streak of consecutive daily sales per product.
;WITH s AS (
    SELECT ProductID, SaleDate,
           DATEADD(DAY,-ROW_NUMBER() OVER(PARTITION BY ProductID ORDER BY SaleDate),
                   SaleDate) grp
    FROM (SELECT DISTINCT ProductID, SaleDate FROM #Sales) d)
SELECT ProductID, MIN(SaleDate) StartD, MAX(SaleDate) EndD, COUNT(*) StreakLen
FROM   s GROUP BY ProductID, grp;

-- Q46. Year-over-year (YoY) sales growth per product (fake by month).
SELECT ProductID, MONTH(SaleDate) Mo,
       SUM(Amount) ThisMo,
       LAG(SUM(Amount)) OVER (PARTITION BY ProductID ORDER BY MONTH(SaleDate)) PrevMo
FROM #Sales GROUP BY ProductID, MONTH(SaleDate);

-- Q47. Top product by total sales using window MAX (no GROUP BY in outer query).
SELECT TOP 1 WITH TIES ProductID, Total
FROM (SELECT ProductID, SUM(Amount) Total FROM #Sales GROUP BY ProductID) x
ORDER BY Total DESC;


/* =============================================================================
   SECTION 5 — PIVOT / UNPIVOT / STRING_AGG / STRING_SPLIT  (Q48 - Q57)
   ============================================================================= */

-- Q48. Pivot total Amount per ProductID across months Jan..Apr.
SELECT *
FROM (SELECT ProductID, MONTH(SaleDate) Mo, Amount FROM #Sales) s
PIVOT (SUM(Amount) FOR Mo IN ([1],[2],[3],[4])) p;

-- Q49. Same pivot but with conditional aggregation (no PIVOT keyword).
SELECT ProductID,
       SUM(CASE WHEN MONTH(SaleDate)=1 THEN Amount END) AS Jan,
       SUM(CASE WHEN MONTH(SaleDate)=2 THEN Amount END) AS Feb,
       SUM(CASE WHEN MONTH(SaleDate)=3 THEN Amount END) AS Mar,
       SUM(CASE WHEN MONTH(SaleDate)=4 THEN Amount END) AS Apr
FROM #Sales GROUP BY ProductID;

-- Q50. Dynamic PIVOT — month columns chosen at runtime.
DECLARE @cols NVARCHAR(MAX), @sql NVARCHAR(MAX);
SELECT @cols = STRING_AGG(QUOTENAME(Mo),',') WITHIN GROUP (ORDER BY Mo)
FROM (SELECT DISTINCT MONTH(SaleDate) Mo FROM #Sales) x;

SET @sql = N'
SELECT * FROM (SELECT ProductID, MONTH(SaleDate) Mo, Amount FROM #Sales) s
PIVOT (SUM(Amount) FOR Mo IN ('+@cols+N')) p;';
EXEC sp_executesql @sql;

-- Q51. UNPIVOT back to long form.
;WITH p AS (
    SELECT ProductID,
           [1] Jan,[2] Feb,[3] Mar,[4] Apr
    FROM (SELECT ProductID, MONTH(SaleDate) Mo, Amount FROM #Sales) s
    PIVOT (SUM(Amount) FOR Mo IN ([1],[2],[3],[4])) p)
SELECT ProductID, MonthName, Amount
FROM   p UNPIVOT (Amount FOR MonthName IN (Jan,Feb,Mar,Apr)) u;

-- Q52. Comma-separate all Products from #TEMP1 in one cell, alphabetized.
SELECT STRING_AGG(Product,', ') WITHIN GROUP (ORDER BY Product) AS AllProducts
FROM   #TEMP1;

-- Q53. Split a CSV string back into rows preserving the input order.
DECLARE @csv NVARCHAR(200) = 'Product D,Product A,Product C';
SELECT value, ordinal
FROM STRING_SPLIT(@csv, ',', 1);   -- 3rd arg = enable_ordinal (SQL 2022+)

-- Q54. Build "key=value;" pairs per product from its sales.
SELECT ProductID,
       STRING_AGG(CONCAT(CONVERT(VARCHAR(10),SaleDate,120),'=',Amount),';')
            WITHIN GROUP (ORDER BY SaleDate) AS KV
FROM   #Sales GROUP BY ProductID;

-- Q55. Reverse a string column without REVERSE() — use STRING_SPLIT + ordinal.
DECLARE @s NVARCHAR(50)='Microsoft';
SELECT STRING_AGG(value,'') WITHIN GROUP (ORDER BY ordinal DESC)
FROM STRING_SPLIT(
    (SELECT STRING_AGG(SUBSTRING(@s,n,1),',') FROM
        (SELECT TOP(LEN(@s)) ROW_NUMBER() OVER(ORDER BY (SELECT 1)) n
         FROM sys.all_objects) x),
    ',',1);

-- Q56. Find products whose name contains a vowel-then-consonant pattern (LIKE).
SELECT * FROM #TEMP1 WHERE Product LIKE '%[aeiou][^aeiou]%';

-- Q57. Case-sensitive search (force binary collation).
SELECT * FROM #TEMP1
WHERE Product COLLATE Latin1_General_BIN LIKE '%A%';


/* =============================================================================
   SECTION 6 — JOINS, SET OPS, APPLY, MERGE  (Q58 - Q72)
   ============================================================================= */

-- Q58. INNER vs LEFT vs FULL join row counts in one shot.
SELECT
    (SELECT COUNT(*) FROM #TEMP1 a INNER JOIN #TEMP2 b ON a.Product=b.Product) Inner_,
    (SELECT COUNT(*) FROM #TEMP1 a LEFT  JOIN #TEMP2 b ON a.Product=b.Product) Left_,
    (SELECT COUNT(*) FROM #TEMP1 a FULL  JOIN #TEMP2 b ON a.Product=b.Product) Full_;

-- Q59. Anti-join: products in #TEMP1 not in #TEMP2 (LEFT JOIN ... IS NULL form).
SELECT a.* FROM #TEMP1 a LEFT JOIN #TEMP2 b
  ON a.Product=b.Product WHERE b.Product IS NULL;

-- Q60. Semi-join: products in #TEMP1 that DO appear in #TEMP2 — use EXISTS.
SELECT * FROM #TEMP1 a
WHERE EXISTS (SELECT 1 FROM #TEMP2 b WHERE b.Product=a.Product);

-- Q61. CROSS JOIN: every product paired with every month 1..4.
SELECT t.Product, m.Mo
FROM #TEMP1 t CROSS JOIN (VALUES(1),(2),(3),(4)) m(Mo);

-- Q62. CROSS APPLY: top 1 sale per product.
SELECT t.ID, t.Product, ca.SaleDate, ca.Amount
FROM #TEMP1 t
CROSS APPLY (SELECT TOP 1 * FROM #Sales s
             WHERE s.ProductID=t.ID ORDER BY s.Amount DESC) ca;

-- Q63. OUTER APPLY: include products with no sales.
SELECT t.ID, t.Product, ca.SaleDate, ca.Amount
FROM #TEMP1 t
OUTER APPLY (SELECT TOP 1 * FROM #Sales s
             WHERE s.ProductID=t.ID ORDER BY s.Amount DESC) ca;

-- Q64. UNION vs UNION ALL: produce the differing counts.
SELECT 'union'     AS k, COUNT(*) c FROM (SELECT Product FROM #TEMP1
                                          UNION SELECT Product FROM #TEMP2) x
UNION ALL
SELECT 'union_all',     COUNT(*)   FROM (SELECT Product FROM #TEMP1
                                          UNION ALL SELECT Product FROM #TEMP2) y;

-- Q65. INTERSECT — products common to both.
SELECT Product FROM #TEMP1 INTERSECT SELECT Product FROM #TEMP2;

-- Q66. Self-join: list pairs of products (a,b) with a.ID < b.ID (combinations).
SELECT a.Product AS P1, b.Product AS P2
FROM   #TEMP1 a JOIN #TEMP1 b ON a.ID < b.ID;

-- Q67. Find missing pairs: products that have never appeared on the same SaleDate.
SELECT a.Product, b.Product
FROM   #TEMP1 a JOIN #TEMP1 b ON a.ID < b.ID
WHERE  NOT EXISTS (
    SELECT 1 FROM #Sales s1 JOIN #Sales s2
      ON s1.SaleDate = s2.SaleDate
     AND s1.ProductID = a.ID AND s2.ProductID = b.ID);

-- Q68. MERGE: upsert #TEMP2 into #TEMP1 by Product (insert new, update price unused).
MERGE #TEMP1 AS tgt
USING (SELECT DISTINCT Product FROM #TEMP2 WHERE Product IS NOT NULL) AS src
ON  tgt.Product = src.Product
WHEN NOT MATCHED BY TARGET THEN INSERT(Product) VALUES(src.Product);

-- Q69. MERGE with OUTPUT capturing $action.
DECLARE @log TABLE(action NVARCHAR(10), Product NVARCHAR(255));
MERGE #TEMP1 tgt
USING (VALUES('Product X'),('Product B')) src(Product)
ON tgt.Product = src.Product
WHEN NOT MATCHED THEN INSERT(Product) VALUES(src.Product)
OUTPUT $action, INSERTED.Product INTO @log;
SELECT * FROM @log;

-- Q70. OUTPUT clause: capture deleted rows.
DECLARE @del TABLE(ID INT, Product NVARCHAR(255));
DELETE FROM #TEMP1
OUTPUT DELETED.ID, DELETED.Product INTO @del
WHERE Product LIKE '%restored%' OR Product='Product X';
SELECT * FROM @del;

-- Q71. EXCEPT vs LEFT JOIN ... IS NULL — produce identical results.
SELECT Product FROM #TEMP1 EXCEPT SELECT Product FROM #TEMP2;

-- Q72. Three-table star join: products x months x running total.
SELECT t.Product, m.Mo,
       SUM(s.Amount) MonthAmt,
       SUM(SUM(s.Amount)) OVER (PARTITION BY t.ID ORDER BY m.Mo) RunTot
FROM #TEMP1 t
CROSS JOIN (VALUES(1),(2),(3),(4)) m(Mo)
LEFT JOIN #Sales s ON s.ProductID=t.ID AND MONTH(s.SaleDate)=m.Mo
GROUP BY t.ID, t.Product, m.Mo
ORDER BY t.ID, m.Mo;


/* =============================================================================
   SECTION 7 — CTEs, RECURSION, TALLY TABLES  (Q73 - Q82)
   ============================================================================= */

-- Q73. Generate numbers 1..100 with a recursive CTE.
;WITH N AS (
    SELECT 1 n UNION ALL SELECT n+1 FROM N WHERE n<100)
SELECT * FROM N OPTION (MAXRECURSION 200);

-- Q74. Generate dates between two endpoints.
DECLARE @d1 DATE='2026-01-01', @d2 DATE='2026-01-10';
;WITH D AS (
    SELECT @d1 d UNION ALL SELECT DATEADD(DAY,1,d) FROM D WHERE d<@d2)
SELECT * FROM D;

-- Q75. Find all subordinates of EmpID=1 (hierarchical recursion).
;WITH org AS (
    SELECT EmpID, EmpName, ManagerID, 0 lvl FROM #Emp WHERE EmpID = 1
    UNION ALL
    SELECT e.EmpID, e.EmpName, e.ManagerID, lvl+1
    FROM #Emp e JOIN org o ON e.ManagerID = o.EmpID)
SELECT * FROM org;

-- Q76. Build org breadcrumb path: "CEO > VP-A > Mgr-A1 > Eng-1".
;WITH org AS (
    SELECT EmpID, CAST(EmpName AS NVARCHAR(MAX)) Path, ManagerID FROM #Emp WHERE ManagerID IS NULL
    UNION ALL
    SELECT e.EmpID, CAST(o.Path + ' > ' + e.EmpName AS NVARCHAR(MAX)), e.ManagerID
    FROM #Emp e JOIN org o ON e.ManagerID = o.EmpID)
SELECT * FROM org ORDER BY Path;

-- Q77. Sum of salaries under each manager (rolled up via recursion).
;WITH sub AS (
    SELECT EmpID, EmpID AS RootID, Salary FROM #Emp
    UNION ALL
    SELECT e.EmpID, s.RootID, e.Salary
    FROM #Emp e JOIN sub s ON e.ManagerID = s.EmpID)
SELECT RootID, SUM(Salary) TotalUnder FROM sub GROUP BY RootID ORDER BY RootID;

-- Q78. Detect cycles in #Emp (artificially: set ManagerID of 1 -> 10 and detect).
-- (Don't actually run UPDATE; the pattern uses MAXRECURSION + cycle check.)
-- SELECT ... FROM cte ... OPTION (MAXRECURSION 50); — error 530 if cycle.

-- Q79. Fibonacci numbers up to 20 terms.
;WITH f AS (
    SELECT 1 n, CAST(0 AS BIGINT) a, CAST(1 AS BIGINT) b
    UNION ALL
    SELECT n+1, b, a+b FROM f WHERE n<20)
SELECT n, a AS fib FROM f;

-- Q80. Tally table from sys.all_objects (a common "no-loop" idiom).
SELECT TOP 1000 ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS n
FROM sys.all_objects a CROSS JOIN sys.all_objects b;

-- Q81. Split a sentence into words using tally + SUBSTRING (no STRING_SPLIT).
DECLARE @t NVARCHAR(200) = 'the quick brown fox jumps';
;WITH t(n) AS (
    SELECT TOP (LEN(@t)) ROW_NUMBER() OVER (ORDER BY (SELECT 1))
    FROM sys.all_objects)
SELECT word = SUBSTRING(@t, n,
       CHARINDEX(' ', @t+' ', n) - n)
FROM t
WHERE n = 1 OR SUBSTRING(@t, n-1, 1) = ' ';

-- Q82. Recursive CTE with MAX recursion override demonstration.
;WITH c AS (
    SELECT 1 n UNION ALL SELECT n+1 FROM c WHERE n<500)
SELECT COUNT(*) FROM c OPTION (MAXRECURSION 0);   -- 0 = unlimited


/* =============================================================================
   SECTION 8 — DATES, MATH, AND TYPE GOTCHAS  (Q83 - Q92)
   ============================================================================= */

-- Q83. First and last day of the current month.
SELECT DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1)              AS FirstDay,
       EOMONTH(GETDATE())                                             AS LastDay;

-- Q84. Age in completed years from a DOB (handles leap years).
DECLARE @dob DATE='2000-02-29';
SELECT DATEDIFF(YEAR,@dob,GETDATE())
       - CASE WHEN DATEADD(YEAR,DATEDIFF(YEAR,@dob,GETDATE()),@dob) > CAST(GETDATE() AS DATE)
              THEN 1 ELSE 0 END AS AgeYrs;

-- Q85. Why does 10/3 return 3 in SQL Server? Fix to get 3.333…
SELECT 10/3 AS IntDiv, 10*1.0/3 AS DecDiv, CAST(10 AS DECIMAL(10,4))/3 AS Explicit;

-- Q86. Implicit conversion trap: '10' + 5 vs '10' + '5'.
SELECT '10' + 5 AS Numeric15, '10' + '5' AS Concat105;

-- Q87. Rounding: ROUND vs CEILING vs FLOOR.
SELECT ROUND(2.5,0), ROUND(3.5,0), CEILING(2.1), FLOOR(2.9);

-- Q88. Convert epoch seconds (1700000000) to a datetime.
SELECT DATEADD(SECOND, 1700000000, '1970-01-01');

-- Q89. Number of business days between two dates (exclude Sat/Sun).
DECLARE @a DATE='2026-01-01', @b DATE='2026-01-31';
SELECT (DATEDIFF(DAY,@a,@b)+1)
       - (DATEDIFF(WEEK,@a,@b)*2)
       - CASE WHEN DATENAME(WEEKDAY,@a)='Sunday'   THEN 1 ELSE 0 END
       - CASE WHEN DATENAME(WEEKDAY,@b)='Saturday' THEN 1 ELSE 0 END AS BizDays;

-- Q90. Generate the last 12 calendar months as a table.
;WITH m AS (
    SELECT 0 i UNION ALL SELECT i+1 FROM m WHERE i<11)
SELECT EOMONTH(DATEADD(MONTH,-i,GETDATE())) AS MonthEnd FROM m;

-- Q91. Format a number with thousand separators (SQL 2012+).
SELECT FORMAT(1234567.89, 'N2', 'en-US');

-- Q92. Safe division (NULL on divide-by-zero) — NULLIF idiom.
SELECT 100.0 / NULLIF(0,0) AS SafeDiv;


/* =============================================================================
   SECTION 9 — JSON, XML, DYNAMIC SQL, TVPs  (Q93 - Q102)
   ============================================================================= */

-- Q93. Return #TEMP1 as a JSON array.
SELECT (SELECT ID, Product FROM #TEMP1 FOR JSON PATH) AS Json1;

-- Q94. Parse a JSON document into rows with OPENJSON.
DECLARE @j NVARCHAR(MAX) = N'[{"ID":1,"P":"A"},{"ID":2,"P":"B"}]';
SELECT * FROM OPENJSON(@j)
WITH (ID INT '$.ID', P NVARCHAR(50) '$.P');

-- Q95. Extract a single JSON property.
SELECT JSON_VALUE(N'{"a":{"b":42}}','$.a.b') AS V;

-- Q96. Modify a JSON property in place.
SELECT JSON_MODIFY(N'{"price":10}','$.price',99) AS Modified;

-- Q97. XML: shred a small XML doc.
DECLARE @x XML = N'<r><p id="1">A</p><p id="2">B</p></r>';
SELECT n.value('@id','int') ID, n.value('.','nvarchar(50)') P
FROM   @x.nodes('/r/p') t(n);

-- Q98. Dynamic SQL with sp_executesql and parameters (NEVER concatenate values).
DECLARE @col SYSNAME='Product', @val NVARCHAR(50)='Product B',
        @sql NVARCHAR(MAX);
SET @sql = N'SELECT * FROM #TEMP1 WHERE ' + QUOTENAME(@col) + N' = @v';
EXEC sp_executesql @sql, N'@v NVARCHAR(50)', @v=@val;

-- Q99. Why is "EXEC(@sql)" with concatenated values an injection risk? Show safe pattern.
-- Answer: any user input embedded as text can terminate the literal and inject code.
-- Always use sp_executesql + typed parameters as in Q98.

-- Q100. Pass a table to a stored proc via a Table-Valued Parameter.
-- (Demonstrates declaration; create proc shown commented out.)
-- CREATE TYPE dbo.ProductList AS TABLE (Product NVARCHAR(255));
-- CREATE PROC dbo.AddProducts @p dbo.ProductList READONLY AS ...

-- Q101. Cursor vs set-based: count rows of #TEMP1 via cursor (demo only).
DECLARE @cnt INT=0, @p NVARCHAR(255);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Product FROM #TEMP1;
OPEN cur; FETCH NEXT FROM cur INTO @p;
WHILE @@FETCH_STATUS = 0
BEGIN SET @cnt+=1; FETCH NEXT FROM cur INTO @p; END
CLOSE cur; DEALLOCATE cur;
SELECT @cnt AS CursorCnt;

-- Q102. Same as Q101 but the right way (set-based).
SELECT COUNT(*) AS SetCnt FROM #TEMP1;


/* =============================================================================
   SECTION 10 — TRANSACTIONS, ISOLATION, ERROR HANDLING  (Q103 - Q112)
   ============================================================================= */

-- Q103. BEGIN TRAN / ROLLBACK demo: changes don't persist.
BEGIN TRAN;
UPDATE #TEMP1 SET Product = Product + '!' WHERE ID=1;
SELECT Product FROM #TEMP1 WHERE ID=1;
ROLLBACK;
SELECT Product FROM #TEMP1 WHERE ID=1;

-- Q104. SAVEPOINT inside a transaction.
BEGIN TRAN;
  UPDATE #TEMP1 SET Product='X' WHERE ID=1;
  SAVE TRAN sp1;
  UPDATE #TEMP1 SET Product='Y' WHERE ID=2;
  ROLLBACK TRAN sp1;        -- undoes only the second UPDATE
COMMIT;
SELECT ID, Product FROM #TEMP1 WHERE ID IN (1,2);

-- Q105. TRY/CATCH with detailed error info.
BEGIN TRY
    DECLARE @x INT = 1/0;
END TRY
BEGIN CATCH
    SELECT ERROR_NUMBER() Num, ERROR_MESSAGE() Msg, ERROR_LINE() Ln,
           ERROR_SEVERITY() Sev, ERROR_STATE() St, ERROR_PROCEDURE() Proc_;
END CATCH;

-- Q106. XACT_ABORT ON — any error rolls back automatically.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO #TEMP1(Product) VALUES('Product A');  -- UNIQUE violation
    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    SELECT ERROR_MESSAGE();
END CATCH;
SET XACT_ABORT OFF;

-- Q107. Isolation level demo (read uncommitted shows dirty rows in other sessions).
-- SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;  -- session-wide
-- SELECT * FROM #TEMP1 WITH (NOLOCK);                -- table hint (avoid in prod)

-- Q108. SNAPSHOT isolation requires DB option; conceptual answer.
-- ALTER DATABASE X SET ALLOW_SNAPSHOT_ISOLATION ON;
-- SET TRANSACTION ISOLATION LEVEL SNAPSHOT;

-- Q109. Detect deadlock victim: ERROR_NUMBER()=1205 in CATCH.
BEGIN TRY
    -- attempt
    SELECT 1;
END TRY
BEGIN CATCH
    IF ERROR_NUMBER()=1205 PRINT 'Deadlock victim - retry';
END CATCH;

-- Q110. THROW vs RAISERROR — modern THROW is the preferred form.
BEGIN TRY THROW 50001,'Custom error',1; END TRY
BEGIN CATCH SELECT ERROR_MESSAGE(); END CATCH;

-- Q111. SET NOCOUNT ON — why it matters for procs (suppresses "n rows affected"
-- messages that confuse ORMs and add network chatter).
SELECT 'best practice: SET NOCOUNT ON;' AS Tip;

-- Q112. @@ROWCOUNT pitfall: only valid for the immediately previous statement.
UPDATE #TEMP1 SET Product=Product WHERE 1=0;
DECLARE @r1 INT = @@ROWCOUNT;        -- 0
SELECT @r1 AS R1;                    -- using @@ROWCOUNT here would be wrong


/* =============================================================================
   SECTION 11 — PERFORMANCE / INDEX-AWARENESS RIDDLES  (Q113 - Q122)
   ============================================================================= */

-- Q113. Why does WHERE YEAR(SaleDate)=2026 prevent index seeks? Rewrite SARGable.
SELECT * FROM #Sales
WHERE  SaleDate >= '2026-01-01' AND SaleDate < '2027-01-01';

-- Q114. Why is WHERE UPPER(Product)='PRODUCT A' non-SARGable? Use a computed/
-- persisted column or store normalized data.
SELECT * FROM #TEMP1
WHERE  Product = 'Product A'   -- compare on the raw column when possible
       COLLATE Latin1_General_CI_AI;

-- Q115. ROW_NUMBER() in a derived table to paginate — the right pattern for OFFSET.
SELECT * FROM (
    SELECT *, ROW_NUMBER() OVER (ORDER BY ID) rn FROM #TEMP1) x
WHERE rn BETWEEN 1 AND 3;

-- Q116. OFFSET/FETCH equivalent.
SELECT * FROM #TEMP1 ORDER BY ID OFFSET 0 ROWS FETCH NEXT 3 ROWS ONLY;

-- Q117. EXISTS vs COUNT(*) > 0 — EXISTS short-circuits.
IF EXISTS(SELECT 1 FROM #Sales WHERE ProductID=1) PRINT 'has sales';

-- Q118. Why is "SELECT * " bad in production? Show explicit column form.
SELECT ID, Product FROM #TEMP1;

-- Q119. NOLOCK shows dirty/missing/duplicated rows — when (if ever) acceptable?
-- Answer: only for non-critical, approximate reads where consistency doesn't matter.

-- Q120. Index hint syntax (use sparingly; for diagnostic experiments).
-- SELECT * FROM #TEMP1 WITH (INDEX(1));

-- Q121. Parameter sniffing: OPTION (RECOMPILE) vs OPTIMIZE FOR UNKNOWN.
-- Demo:
DECLARE @id INT = 2;
SELECT * FROM #TEMP1 WHERE ID=@id OPTION (RECOMPILE);

-- Q122. Estimated vs Actual plan — describe how to enable in SSMS
-- (Ctrl+L = estimated, Ctrl+M = include actual).
SELECT 'Use Ctrl+L / Ctrl+M in SSMS' AS Hint;


/* =============================================================================
   SECTION 12 — SECURITY, METADATA, MISC GOTCHAS  (Q123 - Q132)
   ============================================================================= */

-- Q123. Which schema does a temp table belong to? (tempdb..dbo, despite the # name)
SELECT OBJECT_SCHEMA_NAME(OBJECT_ID('tempdb..#TEMP1'),DB_ID('tempdb')) Schema_;

-- Q124. Difference between #t (local) and ##t (global) temp tables.
-- Local: visible only to the creating session; dropped when session ends or batch ends
-- if created inside a proc. Global: visible to all sessions until last one referencing closes.

-- Q125. Table variable vs #temp — when does the optimizer think table var has 1 row?
-- Pre-2019, always; since 2019 with compat 150 + deferred compilation it sees actuals.

-- Q126. Show all columns of #TEMP1 from metadata.
SELECT c.name, t.name AS DataType, c.max_length, c.is_nullable
FROM   tempdb.sys.columns c
JOIN   tempdb.sys.types   t ON c.user_type_id=t.user_type_id
WHERE  c.object_id = OBJECT_ID('tempdb..#TEMP1');

-- Q127. Get row counts of all user tables in current DB (fast, from DMV).
SELECT t.name, SUM(p.rows) AS Rows_
FROM sys.tables t JOIN sys.partitions p ON p.object_id=t.object_id
WHERE p.index_id IN (0,1) GROUP BY t.name ORDER BY Rows_ DESC;

-- Q128. Show currently running queries (DMV).
SELECT session_id, status, command, blocking_session_id, wait_type
FROM sys.dm_exec_requests WHERE session_id <> @@SPID;

-- Q129. Which session is blocking yours?
SELECT blocking_session_id FROM sys.dm_exec_requests WHERE session_id=@@SPID;

-- Q130. Show last query text for a session via sys.dm_exec_sql_text.
SELECT t.text
FROM   sys.dm_exec_requests r
CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE  r.session_id = @@SPID;

-- Q131. IDENTITY pitfalls: SCOPE_IDENTITY() vs @@IDENTITY vs IDENT_CURRENT.
INSERT INTO #TEMP1(Product) VALUES('Identity Test');
SELECT SCOPE_IDENTITY() Scope_,
       @@IDENTITY       AtAt_,
       IDENT_CURRENT('tempdb..#TEMP1') IdentCur_;

-- Q132. Reseed the IDENTITY counter.
DBCC CHECKIDENT('tempdb..#TEMP1', RESEED, 100);
INSERT INTO #TEMP1(Product) VALUES('Reseeded');
SELECT TOP 2 * FROM #TEMP1 ORDER BY ID DESC;


/* =============================================================================
   SECTION 13 — PUZZLES & EDGE-CASE FAVORITES  (Q133 - Q150)
   ============================================================================= */

-- Q133. Swap values of two columns in a single UPDATE (no temp variable).
-- (Add a stage column for the demo.)
ALTER TABLE #TEMP1 ADD AltName NVARCHAR(255) NULL;
UPDATE #TEMP1 SET AltName='Alt-' + Product WHERE ID<=3;

UPDATE #TEMP1
SET Product = AltName, AltName = Product
WHERE ID<=3;
SELECT TOP 3 * FROM #TEMP1 ORDER BY ID;

-- Q134. Insert returning the IDs of the newly inserted rows.
DECLARE @ins TABLE(NewID INT);
INSERT INTO #TEMP1(Product)
OUTPUT INSERTED.ID INTO @ins
VALUES('Bulk1'),('Bulk2'),('Bulk3');
SELECT * FROM @ins;

-- Q135. Delete every other row (odd IDs only).
-- DELETE FROM #TEMP1 WHERE ID % 2 = 1;       -- run if desired

-- Q136. Find the median ID without PERCENTILE_CONT.
SELECT AVG(1.0*ID) AS Median
FROM (SELECT ID,
             ROW_NUMBER() OVER (ORDER BY ID) rn,
             COUNT(*)    OVER ()            cnt
      FROM #TEMP1) x
WHERE rn IN ((cnt+1)/2, (cnt+2)/2);

-- Q137. Find duplicates that differ ONLY by trailing spaces.
INSERT INTO #TEMP1(Product) VALUES('Product Z'),('Product Z   ');
SELECT Product, LEN(Product) Ln, DATALENGTH(Product) DL
FROM   #TEMP1 WHERE Product LIKE 'Product Z%';

-- Q138. Why doesn't LEN() see trailing spaces? Use DATALENGTH instead.
SELECT LEN('abc   ') Len_, DATALENGTH('abc   ') Bytes_;

-- Q139. Compare two strings ignoring case AND accents.
SELECT CASE WHEN N'café' = N'CAFE' COLLATE Latin1_General_CI_AI
            THEN 'equal' ELSE 'not equal' END;

-- Q140. Pivot WITHOUT the PIVOT keyword AND without aggregation (one row per ProductID).
SELECT t.ID,
       MAX(CASE WHEN MONTH(s.SaleDate)=1 THEN s.Amount END) Jan,
       MAX(CASE WHEN MONTH(s.SaleDate)=2 THEN s.Amount END) Feb
FROM #TEMP1 t LEFT JOIN #Sales s ON s.ProductID=t.ID
GROUP BY t.ID;

-- Q141. ROLLUP / CUBE / GROUPING SETS.
SELECT ProductID, MONTH(SaleDate) Mo, SUM(Amount) Tot
FROM #Sales
GROUP BY ROLLUP(ProductID, MONTH(SaleDate));

-- Q142. GROUPING() to label subtotal rows.
SELECT
    CASE WHEN GROUPING(ProductID)=1 THEN 'ALL' ELSE CAST(ProductID AS VARCHAR) END P,
    SUM(Amount) Tot
FROM #Sales GROUP BY ROLLUP(ProductID);

-- Q143. Conditional aggregation as a substitute for FILTER (which T-SQL lacks).
SELECT SUM(CASE WHEN Qty > 1 THEN Amount END) AS BulkAmount FROM #Sales;

-- Q144. EXISTS-with-correlated-subquery vs IN-with-list — same logical result.
SELECT * FROM #TEMP1 t WHERE EXISTS (SELECT 1 FROM #Sales s WHERE s.ProductID=t.ID);
SELECT * FROM #TEMP1 t WHERE t.ID IN (SELECT ProductID FROM #Sales);

-- Q145. "Latest N per group" using OUTER APPLY (the classic shortcut).
SELECT t.ID, t.Product, x.SaleDate, x.Amount
FROM   #TEMP1 t
OUTER APPLY (SELECT TOP 2 * FROM #Sales s
             WHERE s.ProductID=t.ID ORDER BY s.SaleDate DESC) x;

-- Q146. Detect overlapping date ranges (gaps & islands variant).
;WITH r(s,e) AS (VALUES('2026-01-01','2026-01-10'),
                       ('2026-01-05','2026-01-20'),
                       ('2026-02-01','2026-02-10'))
SELECT a.s, a.e, b.s, b.e
FROM   r a JOIN r b ON a.s < b.e AND b.s < a.e AND a.s <> b.s;

-- Q147. Get every Nth row (e.g., every 2nd).
SELECT * FROM (
    SELECT *, ROW_NUMBER() OVER (ORDER BY ID) rn FROM #TEMP1) x
WHERE rn % 2 = 0;

-- Q148. Replace a value in a column using REPLACE + a JOIN to a mapping table.
;WITH m(old_,new_) AS (VALUES('Product A','PROD-A'),('Product B','PROD-B'))
UPDATE t SET Product = m.new_
FROM #TEMP1 t JOIN m ON t.Product = m.old_;

-- Q149. Apply a lookup MAP via CROSS APPLY (VALUES) inline — no temp table.
SELECT t.ID, t.Product, v.Tier
FROM #TEMP1 t
CROSS APPLY (VALUES (CASE WHEN ID<=2 THEN 'GOLD'
                          WHEN ID<=4 THEN 'SILVER'
                          ELSE 'BRONZE' END)) v(Tier);

-- Q150. ULTIMATE INTERVIEW Q — generate the seed problem's exact output:
--   "Pin ID=2 first, rest in ID order" — and prove it with a CHECKSUM_AGG
--   of the produced ID sequence to compare with the expected hash.
SELECT * FROM #TEMP1
ORDER BY CASE WHEN ID=2 THEN 0 ELSE 1 END, ID;


/* -----------------------------------------------------------------------------
   CLEANUP (optional)
   ----------------------------------------------------------------------------- */
-- DROP TABLE #TEMP1, #TEMP2, #Sales, #Emp;
GO
