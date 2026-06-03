/*
Pre-Deployment Script - Create Encryption Objects
--------------------------------------------------------------------------------------
Creates the encryption hierarchy required for password encryption:
1. Database Master Key (root of encryption)
2. Certificate (protected by master key)
3. Symmetric Key (protected by certificate)
--------------------------------------------------------------------------------------
*/

-- Step 1: Create Database Master Key
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

-- Step 2: Create Certificate for Data Encryption
IF NOT EXISTS (SELECT * FROM sys.certificates WHERE name = 'DataEncryptionCert')
BEGIN
	CREATE CERTIFICATE [DataEncryptionCert]
		AUTHORIZATION [dbo]
		WITH SUBJECT = N'Certificate for Data Encryption',
		START_DATE = N'2026-05-25T17:04:42',
		EXPIRY_DATE = N'2027-05-25T17:04:42';
	PRINT 'DataEncryptionCert certificate created successfully.';
END
ELSE
BEGIN
	PRINT 'DataEncryptionCert certificate already exists.';
END
GO

-- Step 3: Create Symmetric Key for Data Encryption
IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = 'DataEncryptionKey')
BEGIN
	CREATE SYMMETRIC KEY [DataEncryptionKey]
		AUTHORIZATION [dbo]
		WITH ALGORITHM = AES_256
		ENCRYPTION BY CERTIFICATE [DataEncryptionCert];
	PRINT 'DataEncryptionKey symmetric key created successfully.';
END
ELSE
BEGIN
	PRINT 'DataEncryptionKey symmetric key already exists.';
END
GO
