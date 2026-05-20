/* =============================================================================
   19_Data_Modeling_Normalization.sql
   -----------------------------------------------------------------------------
   The "design the schema on a whiteboard" interview round:
       1) Normal forms 1NF -> BCNF (with concrete examples).
       2) When to denormalize on purpose.
       3) OLTP vs OLAP modeling.
       4) Star schema + snowflake schema.
       5) Slowly Changing Dimensions (SCD Type 1/2/3/6).
       6) Surrogate keys vs natural keys.
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) NORMAL FORMS
-- =============================================================================
-- 1NF (First Normal Form):
--   * Every column holds a single atomic value.
--   * No repeating groups (Phone1, Phone2, Phone3) and no comma-separated lists.
--   * Each row uniquely identifiable.
-- Violation:
--   Order(OrderId, Customer, Items = 'Apple,Banana,Cherry')
-- Fix:
--   Order(OrderId, Customer)  +  OrderItem(OrderId, ItemName)
--
-- 2NF: 1NF + every non-key column depends on the WHOLE composite key.
-- Violation (composite key OrderId+ProductId):
--   OrderItem(OrderId, ProductId, ProductName, Qty)   <- ProductName depends only on ProductId
-- Fix:
--   OrderItem(OrderId, ProductId, Qty)  +  Product(ProductId, ProductName)
--
-- 3NF: 2NF + no transitive dependencies (non-key -> non-key).
-- Violation:
--   Employee(EmpId, DeptId, DeptName)   <- DeptName depends on DeptId, not EmpId
-- Fix:
--   Employee(EmpId, DeptId)  +  Department(DeptId, DeptName)
--
-- BCNF (Boyce-Codd): every determinant is a candidate key.
--   Rare in practice; usually achieved alongside 3NF.
--
-- Higher forms (4NF/5NF) handle multi-valued and join dependencies - mostly
-- academic; mention them but don't get lost there.


-- =============================================================================
-- 2) DENORMALIZATION ON PURPOSE
-- =============================================================================
-- Reasons to denormalize:
--   * Read-heavy reporting workloads (avoid join cost).
--   * Caching computed aggregates (RunningTotal, LastLoginDate).
--   * Wide row optimization for columnstore.
-- Tradeoffs:
--   * Update anomalies (must sync duplicates).
--   * Larger storage; more indexes to maintain.
-- Tactics:
--   * Indexed views (materialized).
--   * Computed PERSISTED columns.
--   * Periodic "summary" tables refreshed by ETL.


-- =============================================================================
-- 3) OLTP vs OLAP
-- =============================================================================
-- OLTP (transactional):
--   * Normalized (3NF/BCNF), many small transactions.
--   * Optimize for write throughput + point lookups.
--   * Rowstore + targeted indexes.
--
-- OLAP / Analytics:
--   * Denormalized (star schema).
--   * Bulk reads, aggregations over millions of rows.
--   * Columnstore + partitioning + batch mode.


-- =============================================================================
-- 4) STAR SCHEMA + SNOWFLAKE
-- =============================================================================
-- STAR schema:
--   * One central FACT table (measures + foreign keys).
--   * Many DIMENSION tables (descriptors).
--   * Dimensions are denormalized for fewer joins.
--
-- SNOWFLAKE schema:
--   * Dimensions are normalized into multiple tables (Product -> Category -> Dept).
--   * Saves space; more joins; less common today (storage is cheap).
--
-- Demo (star):
IF OBJECT_ID('dbo.FactOrders',  'U') IS NOT NULL DROP TABLE dbo.FactOrders;
IF OBJECT_ID('dbo.DimDate',     'U') IS NOT NULL DROP TABLE dbo.DimDate;
IF OBJECT_ID('dbo.DimProduct',  'U') IS NOT NULL DROP TABLE dbo.DimProduct;
IF OBJECT_ID('dbo.DimCustomer', 'U') IS NOT NULL DROP TABLE dbo.DimCustomer;

CREATE TABLE dbo.DimDate (
    DateKey       INT          NOT NULL PRIMARY KEY,    -- yyyymmdd
    [Date]        DATE         NOT NULL,
    [Year]        SMALLINT     NOT NULL,
    [Quarter]     TINYINT      NOT NULL,
    [Month]       TINYINT      NOT NULL,
    [DayOfWeek]   TINYINT      NOT NULL,
    IsWeekend     BIT          NOT NULL
);

CREATE TABLE dbo.DimProduct (
    ProductKey    INT IDENTITY(1,1) PRIMARY KEY,
    ProductId     INT          NOT NULL,                -- business / natural key
    Name          NVARCHAR(100) NOT NULL,
    Category      NVARCHAR(50)  NOT NULL,               -- denormalized into Dim
    Department    NVARCHAR(50)  NOT NULL
);

CREATE TABLE dbo.DimCustomer (
    CustomerKey   INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId    INT          NOT NULL,
    Name          NVARCHAR(100) NOT NULL,
    City          NVARCHAR(100) NOT NULL,
    Country       NVARCHAR(50)  NOT NULL,
    ValidFrom     DATETIME2     NOT NULL,
    ValidTo       DATETIME2     NULL,
    IsCurrent     BIT           NOT NULL
);

CREATE TABLE dbo.FactOrders (
    OrderKey      BIGINT IDENTITY(1,1) PRIMARY KEY,
    DateKey       INT          NOT NULL REFERENCES dbo.DimDate(DateKey),
    ProductKey    INT          NOT NULL REFERENCES dbo.DimProduct(ProductKey),
    CustomerKey   INT          NOT NULL REFERENCES dbo.DimCustomer(CustomerKey),
    Quantity      INT          NOT NULL,
    Amount        DECIMAL(12,2) NOT NULL
);


-- =============================================================================
-- 5) SLOWLY CHANGING DIMENSIONS (SCD)
-- =============================================================================
-- Type 1: OVERWRITE. No history. Easy, lossy.
--         UPDATE DimCustomer SET City = @newCity WHERE CustomerId = @id;
--
-- Type 2: NEW ROW per change. Full history. Most common for analytics.
--         Add ValidFrom / ValidTo / IsCurrent (already in DimCustomer above).
--         Update closes the old row, INSERT opens a new one. Surrogate key
--         (CustomerKey) differs; business key (CustomerId) stays the same.
--
-- Type 3: NEW COLUMN per change (PreviousCity / CurrentCity). Limited history,
--         fixed depth. Niche use.
--
-- Type 6: 1+2+3 combined - overwrite a "current" column AND keep a versioned
--         history row. Best of both worlds for reporting.

-- SCD Type 2 update pattern:
-- BEGIN TRAN;
--   UPDATE dbo.DimCustomer
--      SET ValidTo = SYSUTCDATETIME(), IsCurrent = 0
--    WHERE CustomerId = @id AND IsCurrent = 1;
--
--   INSERT INTO dbo.DimCustomer (CustomerId, Name, City, Country, ValidFrom, ValidTo, IsCurrent)
--   VALUES (@id, @name, @newCity, @country, SYSUTCDATETIME(), NULL, 1);
-- COMMIT;


-- =============================================================================
-- 6) SURROGATE vs NATURAL KEYS
-- =============================================================================
-- SURROGATE key (IDENTITY / SEQUENCE / GUID):
--   * Stable, narrow, internal-only.
--   * Required for SCD Type 2 (business key reused across versions).
--   * Decouples the model from real-world key changes (email change, SSN reuse).
--
-- NATURAL key (Email, ISBN, SSN):
--   * Human-meaningful.
--   * Risk: real-world keys CHANGE or get reused. Hard to fix once foreign-keyed.
--
-- Best practice: PRIMARY KEY = surrogate; add a UNIQUE constraint on the
-- natural key for data integrity + lookup performance.


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * 1NF atomic; 2NF whole key; 3NF nothing but the key.
   * OLTP -> normalize; OLAP -> star schema + columnstore.
   * SCD Type 1 = overwrite, Type 2 = new row with effective dates (most common).
   * Always prefer surrogate PK + UNIQUE on natural key.
   * Denormalize deliberately, with a refresh strategy.
   ============================================================================= */
