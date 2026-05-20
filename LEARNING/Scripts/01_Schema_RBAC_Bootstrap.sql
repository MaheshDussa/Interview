/* =============================================================================
   Script1.sql  -  Schema bootstrap for the LEARNING database
   -----------------------------------------------------------------------------
   Purpose : Create the LEARNING database and the core RBAC (Role Based Access
             Control) schema:
                  USERS  -- USER_ROLES --  ROLES  -- ROLE_PERMISSION_MAPPING --  ROLE_PERMISSIONS

   Interview talking points:
     * IDENTITY(1,1) auto-generates surrogate primary keys (seed=1, step=1).
     * Composite PRIMARY KEY on junction tables (USER_ROLES,
       ROLE_PERMISSION_MAPPING) prevents duplicate assignments and is the
       standard way to model a many-to-many relationship.
     * UNIQUE constraints on Email/Phone enforce business uniqueness; this is
       different from a PRIMARY KEY (PK = unique + NOT NULL + usually clustered).
     * HASHBYTES('SHA2_256', ...) stores password hashes - never plain text.
       VARBINARY(64) leaves headroom for a 32-byte SHA-256 digest.
     * CHECK constraint on Email gives a cheap server-side format guard
       (defense-in-depth alongside app validation).
   ============================================================================= */


-- Create the database (runs in master context). Comment out on re-runs.
CREATE DATABASE LEARNING;
GO

-- Switch session context so all subsequent DDL targets LEARNING, not master.
USE  LEARNING;
GO

-- DANGER: Drops the whole database (schema + data). Kept only for local
-- teardown during practice. NEVER leave an un-guarded DROP DATABASE in prod.
DROP DATABASE LEARNING;
GO


/* -----------------------------------------------------------------------------
   USERS - master table of application users (the "principals").
   ----------------------------------------------------------------------------- */
CREATE TABLE [USERS] (
UserId INT PRIMARY KEY IDENTITY(1,1),          -- Surrogate PK, auto-increment
FirstName NVARCHAR(100) NULL,                  -- NVARCHAR = Unicode (supports i18n names)
LastName NVARCHAR (100) NULL,
Email NVARCHAR(100) NOT NULL UNIQUE,           -- Natural key; UNIQUE => one account per email
Phone NVARCHAR(13) NULL  UNIQUE,               -- 13 chars: "+" + country code + number
PasswordHash VARBINARY NULL,                   -- NOTE: default VARBINARY length is 1 byte - resized below via ALTER
IsActive BIT DEFAULT 0,                        -- Soft-enable flag; new users inactive until verified
CreatedDate DATETIME DEFAULT GETDATE()         -- Audit column populated server-side
);
GO

/* -----------------------------------------------------------------------------
   ROLES - lookup table of role names (Admin, Manager, ...).
   ----------------------------------------------------------------------------- */
CREATE TABLE [ROLES] (
RoleId INT PRIMARY KEY IDENTITY(1,1),
RoleName NVARCHAR(50) NOT NULL                 -- Could add UNIQUE to forbid duplicate role names
);
GO

/* -----------------------------------------------------------------------------
   USER_ROLES - junction table resolving the many-to-many USERS <-> ROLES.
   Composite PK (UserId, RoleId): a user can hold many roles, a role can be
   held by many users, but the same pair cannot exist twice.
   ----------------------------------------------------------------------------- */
CREATE TABLE [USER_ROLES] (
UserId INT NOT NULL,
RoleId INT NOT NULL,
PRIMARY KEY (UserId, RoleId),                  -- Composite key prevents duplicate assignments
FOREIGN KEY (UserId) REFERENCES USERS(UserId), -- Referential integrity to USERS
FOREIGN KEY (RoleId) REFERENCES ROLES(RoleId)  -- Referential integrity to ROLES
)

GO

/* -----------------------------------------------------------------------------
   ROLE_PERMISSIONS - catalog of permission strings (Create User, View Logs ...).
   ----------------------------------------------------------------------------- */
CREATE TABLE [ROLE_PERMISSIONS] (
PermissionId INT PRIMARY KEY IDENTITY(1,1),
Name NVARCHAR(50) NOT NULL,
) ;
GO

/* -----------------------------------------------------------------------------
   ROLE_PERMISSION_MAPPING - junction between ROLES and ROLE_PERMISSIONS.
   This turns RBAC from just "role names" into real authorization rules.
   ----------------------------------------------------------------------------- */
CREATE TABLE [ROLE_PERMISSION_MAPPING] (

RoleId INT NOT NULL,
PermissionId INT NOT NULL,
PRIMARY KEY (RoleId, PermissionId),                                  -- Composite key, same reasoning as USER_ROLES
FOREIGN KEY (RoleId) REFERENCES ROLES(RoleId),
FOREIGN KEY (PermissionId) REFERENCES ROLE_PERMISSIONS(PermissionId)
);
go


/* -----------------------------------------------------------------------------
   Post-creation schema tweaks (ALTER) - shows how to evolve a live table,
   which is the realistic case in production.
   ----------------------------------------------------------------------------- */

-- Add a CHECK constraint enforcing a minimal email shape ("x@y.z").
-- Server-side validation = defense-in-depth, even if the app already validates.
ALTER TABLE [USERS] 
ADD CONSTRAINT CHK_Email CHECK (Email LIKE '%@%.%');


-- Fix the PasswordHash size. Original VARBINARY (no length) defaults to 1 byte,
-- which would truncate a SHA-256 (32-byte) hash. VARBINARY(64) leaves headroom.
ALTER TABLE [USERS]
ALTER COLUMN PasswordHash VARBINARY(64) NULL;


/* -----------------------------------------------------------------------------
   DROP vs TRUNCATE vs DELETE - classic interview question.
     DROP TABLE     -> removes definition + data + indexes + constraints.
     TRUNCATE TABLE -> keeps the table, removes ALL rows, resets IDENTITY,
                       minimally logged, blocked if an FK references the table.
     DELETE         -> row-by-row, fully logged, supports WHERE filter.
   ----------------------------------------------------------------------------- */
DROP  TABLE IF EXISTS [ROLE_PERMISSIONS];   -- "IF EXISTS" avoids an error when already dropped
GO 
TRUNCATE TABLE [ROLE_PERMISSIONS];          -- Would fail while ROLE_PERMISSION_MAPPING FK is active



/* =============================================================================
   SEED DATA
   The inserts below intentionally create "gaps" so the dataset is useful for
   practicing every JOIN flavor:
     * Users without roles       -> exercise LEFT  JOIN
     * Roles without users       -> exercise RIGHT JOIN
     * Roles without permissions -> exercise LEFT  JOIN on mapping
     * Permissions without roles -> exercise RIGHT JOIN on mapping
   ============================================================================= */

-- Insert sample data into USERS table.
-- HASHBYTES('SHA2_256', '<plain>') is for demo data only; a real system would
-- also use a per-user salt and a slow KDF (bcrypt/argon2) at the app tier.
INSERT INTO [USERS] (FirstName, LastName, Email, Phone, PasswordHash, IsActive)
VALUES 
    ('John', 'Doe', 'john.doe@email.com', '1234567890', HASHBYTES('SHA2_256', 'password1'), 1),
    ('Jane', 'Smith', 'jane.smith@email.com', '2345678901', HASHBYTES('SHA2_256', 'password2'), 1),
    ('Mike', 'Johnson', 'mike.j@email.com', '3456789012', HASHBYTES('SHA2_256', 'password3'), 1),
    ('Sarah', 'Williams', 'sarah.w@email.com', '4567890123', HASHBYTES('SHA2_256', 'password4'), 0),   -- inactive user (IsActive = 0)
    ('David', 'Brown', 'david.b@email.com', '5678901234', HASHBYTES('SHA2_256', 'password5'), 1),
    ('Emily', 'Davis', 'emily.d@email.com', '6789012345', HASHBYTES('SHA2_256', 'password6'), 1),
    ('Chris', 'Miller', 'chris.m@email.com', '7890123456', HASHBYTES('SHA2_256', 'password7'), 0),     -- inactive user
    ('Anna', 'Wilson', 'anna.w@email.com', '8901234567', HASHBYTES('SHA2_256', 'password8'), 1),
    ('Tom', 'Moore', 'tom.m@email.com', '9012345678', HASHBYTES('SHA2_256', 'password9'), 1),
    ('Lisa', 'Taylor', 'lisa.t@email.com', '0123456789', HASHBYTES('SHA2_256', 'password10'), 1),
    ('Mark', 'Anderson', 'mark.a@email.com', '1122334455', HASHBYTES('SHA2_256', 'password11'), 0),
    ('Rachel', 'Thomas', 'rachel.t@email.com', '2233445566', HASHBYTES('SHA2_256', 'password12'), 1);
GO

-- Insert sample data into ROLES table (the role catalog).
INSERT INTO [ROLES] (RoleName)
VALUES 
    ('Administrator'),
    ('Manager'),
    ('Developer'),
    ('Analyst'),
    ('Support'),
    ('Guest'),
    ('Auditor'),
    ('Super Admin');
GO

-- Insert sample data into USER_ROLES.
-- Gaps are intentional: some users have no roles, some roles have no users.
INSERT INTO [USER_ROLES] (UserId, RoleId)
VALUES 
    (1, 1),  -- John  -> Administrator
    (1, 8),  -- John  -> Super Admin    (a single user holding MULTIPLE roles)
    (2, 2),  -- Jane  -> Manager
    (3, 3),  -- Mike  -> Developer
    (4, 3),  -- Sarah -> Developer
    (5, 4),  -- David -> Analyst
    (6, 2),  -- Emily -> Manager
    (7, 5),  -- Chris -> Support
    (8, 3),  -- Anna  -> Developer
    (9, 4);  -- Tom   -> Analyst
    -- Lisa (10), Mark (11), Rachel (12) are UNASSIGNED  -> LEFT JOIN  practice (USERS LEFT JOIN USER_ROLES)
    -- Guest (6) and Auditor (7) have no users          -> RIGHT JOIN practice (USER_ROLES RIGHT JOIN ROLES)
GO

-- Insert sample data into ROLE_PERMISSIONS (the permission catalog).
INSERT INTO [ROLE_PERMISSIONS] (Name)
VALUES 
    ('Create User'),
    ('Delete User'),
    ('Edit User'),
    ('View Reports'),
    ('Generate Reports'),
    ('Manage System'),
    ('View Logs'),
    ('Export Data'),
    ('Import Data'),
    ('Backup Database');
GO

-- Insert sample data into ROLE_PERMISSION_MAPPING.
-- Again, gaps are deliberate to make JOIN exercises meaningful.
INSERT INTO [ROLE_PERMISSION_MAPPING] (RoleId, PermissionId)
VALUES 
    (1, 1),  -- Administrator -> Create User
    (1, 2),  -- Administrator -> Delete User
    (1, 3),  -- Administrator -> Edit User
    (1, 6),  -- Administrator -> Manage System
    (8, 1),  -- Super Admin   -> Create User
    (8, 2),  -- Super Admin   -> Delete User
    (8, 3),  -- Super Admin   -> Edit User
    (8, 6),  -- Super Admin   -> Manage System
    (8, 10), -- Super Admin   -> Backup Database   (only Super Admin owns this one)
    (2, 4),  -- Manager       -> View Reports
    (2, 5),  -- Manager       -> Generate Reports
    (2, 8),  -- Manager       -> Export Data
    (3, 7),  -- Developer     -> View Logs
    (3, 9),  -- Developer     -> Import Data
    (4, 4),  -- Analyst       -> View Reports
    (4, 8);  -- Analyst       -> Export Data
    -- Support (5), Guest (6), Auditor (7) have NO permissions  -> LEFT JOIN  practice (ROLES LEFT JOIN ROLE_PERMISSION_MAPPING)
    -- Permissions unassigned to any role                       -> RIGHT JOIN practice (ROLE_PERMISSION_MAPPING RIGHT JOIN ROLE_PERMISSIONS)
GO
 
