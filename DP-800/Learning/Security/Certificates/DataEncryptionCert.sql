/*
	Script:     DataEncryptionCert.sql
	Purpose:    Creates certificate for encrypting symmetric keys
	Author:     Database Administrator
*/

PRINT 'Checking DataEncryptionCert certificate...';

IF NOT EXISTS (SELECT 1 FROM sys.certificates WHERE [name] = 'DataEncryptionCert')
BEGIN
	PRINT 'Creating DataEncryptionCert certificate...';
	CREATE CERTIFICATE [DataEncryptionCert]
		WITH SUBJECT = N'Certificate for Data Encryption';
	PRINT 'DataEncryptionCert certificate created successfully.';
END
ELSE
BEGIN
	PRINT 'DataEncryptionCert certificate already exists.';
END
GO
