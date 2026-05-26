/*
	Script:     CreateMasterKey.sql
	Purpose:    Creates Database Master Key if it does not exist
	Author:     Database Administrator
*/

PRINT 'Checking Database Master Key...';

IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE [name] = '##MS_DatabaseMasterKey##')
BEGIN
	PRINT 'Creating Database Master Key...';
	CREATE MASTER KEY ENCRYPTION BY PASSWORD = N'$(MasterKeyPassword)';
	PRINT 'Database Master Key created successfully.';
END
ELSE
BEGIN
	PRINT 'Database Master Key already exists.';
END
GO
