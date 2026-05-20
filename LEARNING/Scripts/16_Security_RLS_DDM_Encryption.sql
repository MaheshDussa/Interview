/* =============================================================================
   16_Security_RLS_DDM_Encryption.sql
   -----------------------------------------------------------------------------
   Security model + the four big "data protection" features:
       1) Principals : logins (server) vs users (DB) vs schemas vs roles.
       2) Permissions: GRANT / DENY / REVOKE; ownership chaining; EXECUTE AS.
       3) Row-Level Security (RLS)            - filter rows by predicate.
       4) Dynamic Data Masking (DDM)          - mask columns at query time.
       5) Encryption: TDE / Always Encrypted / column-level / TLS.
   ============================================================================= */

USE LEARNING;
GO

-- Make sure the Security schema exists (used later for the RLS predicate fn).
IF SCHEMA_ID('Security') IS NULL
    EXEC ('CREATE SCHEMA Security AUTHORIZATION dbo');
GO

-- =============================================================================
-- 1) PRINCIPALS  (who you are)
-- =============================================================================
-- LOGIN     : server-level identity (SQL or Windows / Entra).
-- USER      : database-level identity, usually mapped to a login.
-- SCHEMA    : container/namespace for objects (dbo, sales, hr, ...).
-- ROLE      : group of users that share permissions (db_datareader, custom).
--
-- Best practice: grant permissions to ROLES, not individual users. Add/remove
-- users from roles to manage access at scale.

-- Inspect current security context:
SELECT SUSER_NAME()     AS server_login,
       USER_NAME()      AS db_user,
       ORIGINAL_LOGIN() AS original_login,
       SESSION_USER     AS [session_user];


-- =============================================================================
-- 2) PERMISSIONS  (what you can do)
-- =============================================================================
-- Standard pattern (commented - run only with admin rights):
-- CREATE LOGIN AppLogin WITH PASSWORD = 'S3cret!';
-- CREATE USER  AppUser  FOR LOGIN AppLogin;
-- CREATE ROLE  AppReader;
-- ALTER ROLE   AppReader ADD MEMBER AppUser;
-- GRANT SELECT ON SCHEMA::dbo TO AppReader;       -- schema-level grant
-- DENY  SELECT ON dbo.USERS (PasswordHash) TO AppReader;   -- column-level deny
--
-- Ordering: DENY beats GRANT. REVOKE removes both.
--
-- OWNERSHIP CHAINING:
--   When a proc owned by user X selects from a table owned by user X,
--   the caller does NOT need direct SELECT on the table - just EXECUTE on
--   the proc. This is how you expose data via procs while locking down
--   underlying tables.
--
-- EXECUTE AS:
--   Run a proc under a different security context. Lets you grant
--   "do X via this proc" without granting the underlying permissions.
--   Common: EXECUTE AS OWNER / EXECUTE AS 'PrivilegedUser'.


-- =============================================================================
-- 3) ROW-LEVEL SECURITY (RLS)
-- =============================================================================
-- Filters rows transparently based on a predicate FUNCTION you write.
-- Two policy types:
--   FILTER PREDICATE  - hides rows from SELECT/UPDATE/DELETE.
--   BLOCK  PREDICATE  - blocks INSERT/UPDATE/DELETE that would violate.

IF OBJECT_ID('dbo.SalesByRep','U') IS NOT NULL DROP TABLE dbo.SalesByRep;
CREATE TABLE dbo.SalesByRep
(
    SaleId    INT IDENTITY(1,1) PRIMARY KEY,
    SalesRep  SYSNAME       NOT NULL,    -- login name of the rep
    Amount    DECIMAL(12,2) NOT NULL
);

INSERT INTO dbo.SalesByRep (SalesRep, Amount) VALUES
(N'alice', 100), (N'alice', 50), (N'bob', 200), (N'carol', 75);
GO

-- A SECURITY POLICY cannot reference a function it already depends on while
-- you're trying to recreate it - drop the policy first, then the function.
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = 'SalesFilter')
    DROP SECURITY POLICY Security.SalesFilter;
IF OBJECT_ID('Security.fn_SalesRepPredicate', 'IF') IS NOT NULL
    DROP FUNCTION Security.fn_SalesRepPredicate;
GO

-- The predicate function decides which rows are visible.
CREATE FUNCTION Security.fn_SalesRepPredicate (@SalesRep AS SYSNAME)
RETURNS TABLE   
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS allowed
    WHERE  @SalesRep = USER_NAME()                          -- only your rows
       OR  IS_MEMBER(N'db_owner') = 1                       -- admins see all
       OR  IS_MEMBER(N'SalesManagerRole') = 1;              -- sales managers see all
GO

-- Attach the predicate to the table as a SECURITY POLICY.
CREATE SECURITY POLICY Security.SalesFilter
    ADD FILTER PREDICATE Security.fn_SalesRepPredicate(SalesRep) ON dbo.SalesByRep,
    ADD BLOCK  PREDICATE Security.fn_SalesRepPredicate(SalesRep) ON dbo.SalesByRep AFTER INSERT
WITH (STATE = ON);
GO


-- =============================================================================
-- 4) DYNAMIC DATA MASKING (DDM)
-- =============================================================================
-- COSMETIC masking of column data at SELECT time for non-privileged users.
-- The data on disk is unchanged. Users WITH UNMASK permission see the real
-- values. NOT a security boundary by itself (predicate WHERE col = '...'
-- can still leak values) - layer with RLS / column permissions.
--
-- Built-in masks: default(), email(), partial(prefix,padding,suffix),
-- random(low, high).

IF OBJECT_ID('dbo.Customer','U') IS NOT NULL DROP TABLE dbo.Customer;
CREATE TABLE dbo.Customer
(
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    FullName   NVARCHAR(100) MASKED WITH (FUNCTION = 'partial(1,"xxx",1)'),
    Email      NVARCHAR(100) MASKED WITH (FUNCTION = 'email()'),
    SSN        CHAR(11)      MASKED WITH (FUNCTION = 'default()'),
    Salary     DECIMAL(10,2) MASKED WITH (FUNCTION = 'random(50000, 99999)')
);

INSERT INTO dbo.Customer (FullName, Email, SSN, Salary) VALUES
(N'John Smith', N'john@email.com', '111-22-3333', 75000);
GO

-- See the data WITH the masks (run as a non-privileged user to see masks):
SELECT * FROM dbo.Customer;

-- Grant a user the right to bypass masking:
-- GRANT UNMASK TO SomeUser;
-- REVOKE UNMASK FROM SomeUser;


-- =============================================================================
-- 5) ENCRYPTION OPTIONS  (know the differences)
-- =============================================================================
-- TDE (Transparent Data Encryption):
--   * Encrypts data files + backups on disk.
--   * Transparent to apps; SQL Server handles encrypt/decrypt at IO time.
--   * Does NOT protect data in flight or in memory.
--
-- Always Encrypted:
--   * Client driver encrypts/decrypts. SQL Server NEVER sees plaintext.
--   * Two flavors: Deterministic (supports equality lookups) and Randomized
--     (more secure, but no comparisons).
--   * Use for highly sensitive columns (SSN, credit card).
--
-- Column-level encryption (legacy):
--   * Symmetric/asymmetric keys + EncryptByKey / DecryptByKey functions.
--   * App must manage OPEN SYMMETRIC KEY before using.
--
-- TLS:
--   * Encrypts traffic between client and server.
--   * Force with ENCRYPT=true; / TrustServerCertificate=false in connection strings.


/* =============================================================================
   QUICK INTERVIEW REVIEW
   * Grant to ROLES; combine GRANT/DENY (DENY wins).
   * Ownership chaining lets procs read tables without granting table access.
   * RLS  - hides rows via predicate functions (FILTER vs BLOCK).
   * DDM  - cosmetic masking; NOT a real security boundary by itself.
   * TDE = on-disk transparent; Always Encrypted = client-side, SQL never decrypts.
   ============================================================================= */
