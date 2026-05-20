/* =============================================================================
   12_Constraints_Computed_Columns_Sequences.sql
   -----------------------------------------------------------------------------
   Constraints enforce business rules INSIDE the database. They survive bad
   client code, ad-hoc updates, and ETL jobs - which is why interviewers love
   them.

   The 6 constraint types:
       PRIMARY KEY  - uniqueness + NOT NULL + clustered (by default).
       UNIQUE       - uniqueness only (allows one NULL by default in SQL Server).
       FOREIGN KEY  - referential integrity to a parent table.
       CHECK        - boolean expression must be TRUE/UNKNOWN per row.
       DEFAULT      - value when INSERT omits the column.
       NOT NULL     - column-level required value.

   Identity vs Sequence (interview classic):
       IDENTITY  - per-column auto increment, simple but limited (one per table,
                   can't reset without RESEED, no cross-table sharing).
       SEQUENCE  - standalone schema object, cross-table, restartable,
                   supports NEXT VALUE FOR, CACHE/NO CACHE, CYCLE.
   ============================================================================= */

USE LEARNING;
GO

-- =============================================================================
-- 1) ALL CONSTRAINTS IN ONE TABLE
-- =============================================================================
IF OBJECT_ID('dbo.Product',  'U') IS NOT NULL DROP TABLE dbo.Product;
IF OBJECT_ID('dbo.Category', 'U') IS NOT NULL DROP TABLE dbo.Category;

CREATE TABLE dbo.Category
(
    CategoryId INT          NOT NULL,
    Name       NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_Category   PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Category   UNIQUE (Name)             -- alternate key
);

CREATE TABLE dbo.Product
(
    ProductId   INT IDENTITY(1,1)  NOT NULL,
    Sku         VARCHAR(20)        NOT NULL,
    CategoryId  INT                NOT NULL,
    Price       DECIMAL(10,2)      NOT NULL,
    Discount    DECIMAL(5,2)       NOT NULL DEFAULT 0,
    IsActive    BIT                NOT NULL DEFAULT 1,
    CreatedUtc  DATETIME2          NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Product       PRIMARY KEY (ProductId),
    CONSTRAINT UQ_Product_Sku   UNIQUE (Sku),
    CONSTRAINT FK_Product_Cat   FOREIGN KEY (CategoryId)
        REFERENCES dbo.Category (CategoryId)
        ON UPDATE CASCADE                       -- parent key change cascades to child
        ON DELETE NO ACTION,                    -- delete is blocked if children exist
    CONSTRAINT CK_Product_Price CHECK (Price    >= 0),
    CONSTRAINT CK_Product_Disc  CHECK (Discount BETWEEN 0 AND Price)   -- can compare two columns
);
GO


-- =============================================================================
-- 2) FOREIGN KEY referential actions (interview cheat sheet)
-- =============================================================================
--   NO ACTION   - reject parent change if any child rows reference it (default).
--   CASCADE     - apply same change (delete/update) to child rows.
--   SET NULL    - set child FK column to NULL (FK column must be NULLable).
--   SET DEFAULT - set child FK column to its DEFAULT.
-- Gotchas:
--   * MERGE / multiple cascade paths can be blocked ("circular cascade").
--   * Cascading deletes can silently wipe a lot of data - prefer NO ACTION + app-level cleanup.


-- =============================================================================
-- 3) COMPUTED COLUMNS (virtual + persisted)
-- =============================================================================
-- A COMPUTED column is derived from an expression. By default it is
-- "virtual" (recomputed at query time). Adding PERSISTED stores it on disk
-- and lets you INDEX it or use it as a FOREIGN KEY column.
ALTER TABLE dbo.Product
ADD NetPrice AS (Price - Discount);                            -- virtual

ALTER TABLE dbo.Product
ADD NetPricePersisted AS (Price - Discount) PERSISTED;          -- materialized

CREATE NONCLUSTERED INDEX IX_Product_NetPrice
    ON dbo.Product (NetPricePersisted);
GO


-- =============================================================================
-- 4) IDENTITY vs SEQUENCE
-- =============================================================================
-- IDENTITY recap (column-bound):
--   * One per table.
--   * DBCC CHECKIDENT('dbo.Product', RESEED, 1000);  -- reset next value
--   * @@IDENTITY / SCOPE_IDENTITY() / IDENT_CURRENT('...')  - retrieve last id.
--   * SCOPE_IDENTITY() is the safe choice (limited to current scope + session).

-- SEQUENCE (standalone object):
IF EXISTS (SELECT 1 FROM sys.sequences WHERE name = 'OrderNumberSeq')
    DROP SEQUENCE dbo.OrderNumberSeq;

CREATE SEQUENCE dbo.OrderNumberSeq
    AS BIGINT
    START WITH 1000
    INCREMENT BY 1
    MINVALUE 1000
    MAXVALUE 9999999999
    NO CYCLE
    CACHE 50;       -- pre-allocate 50 values for perf (can lose values on crash)

-- Grab the next value (consumes it).
SELECT NEXT VALUE FOR dbo.OrderNumberSeq AS NextOrderNumber;

-- Use as a DEFAULT on a column - shared across tables if you need it.
ALTER TABLE dbo.Product
ADD OrderNumberHint BIGINT NULL CONSTRAINT DF_Product_OrderNumberHint
    DEFAULT (NEXT VALUE FOR dbo.OrderNumberSeq);
GO


-- =============================================================================
-- 5) Enabling / disabling constraints (bulk loads, data fixes)
-- =============================================================================
-- Disable check + FK constraints during a load, then re-enable WITH CHECK
-- so existing data is validated again.
-- ALTER TABLE dbo.Product NOCHECK CONSTRAINT ALL;
--   ... bulk load ...
-- ALTER TABLE dbo.Product WITH CHECK CHECK CONSTRAINT ALL;
--
-- A "trusted" constraint (is_not_trusted = 0) is useful to the optimizer
-- (it can use it to eliminate joins / predicates). NOCHECK leaves it
-- "not trusted" until you reapply WITH CHECK.
SELECT name, is_disabled, is_not_trusted
FROM   sys.foreign_keys
WHERE  parent_object_id = OBJECT_ID('dbo.Product');


-- =============================================================================
-- 6) Inspecting all constraints on a table
-- =============================================================================
SELECT  c.name AS constraint_name, c.type_desc, OBJECT_NAME(c.parent_object_id) AS table_name
FROM    sys.objects c
WHERE   c.parent_object_id = OBJECT_ID('dbo.Product')
  AND   c.type IN ('PK','UQ','F','C','D')
ORDER  BY c.type_desc;


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * Always enforce business rules in the DB layer (CHECK/FK/UQ) - apps lie.
   * PK = NOT NULL + UNIQUE; UNIQUE allows ONE null in SQL Server.
   * FK referential actions: NO ACTION (default), CASCADE, SET NULL, SET DEFAULT.
   * PERSISTED computed columns can be indexed and used in FKs.
   * SEQUENCE > IDENTITY when you need cross-table, restartable, or pre-fetched IDs.
   * Use SCOPE_IDENTITY() (not @@IDENTITY) to get the last inserted IDENTITY.
   ============================================================================= */
