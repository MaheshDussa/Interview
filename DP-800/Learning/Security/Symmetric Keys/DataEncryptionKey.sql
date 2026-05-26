/*
	Script:     DataEncryptionKey.sql
	Purpose:    Creates AES-256 symmetric key for data encryption
	Author:     Database Administrator
*/

PRINT 'Checking DataEncryptionKey symmetric key...';

IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE [name] = 'DataEncryptionKey')
BEGIN
	PRINT 'Creating DataEncryptionKey symmetric key...';
	CREATE SYMMETRIC KEY [DataEncryptionKey]
		WITH ALGORITHM = AES_256
		ENCRYPTION BY CERTIFICATE [DataEncryptionCert];
	PRINT 'DataEncryptionKey symmetric key created successfully.';
END
ELSE
BEGIN
	PRINT 'DataEncryptionKey symmetric key already exists.';
END
GO
