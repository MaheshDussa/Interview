-- =============================================
-- Create Database Master Key
-- Must be created BEFORE any certificates
-- =============================================
-- The Database Master Key must exist before creating certificates
-- This is the root of the encryption hierarchy

IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
	CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'StrongP@ssw0rd!2026';
	PRINT 'Database Master Key created successfully.';
END
ELSE
BEGIN
	PRINT 'Database Master Key already exists.';
END
GO
