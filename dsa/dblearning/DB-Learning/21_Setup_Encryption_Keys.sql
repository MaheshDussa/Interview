/*
=====================================================================================
Script: Setup Symmetric Key Encryption Infrastructure
Description: Creates database master key, certificate, and symmetric key for 
			 password encryption/decryption
Author: System Generated
Date: Created on deployment
=====================================================================================
*/

-- Step 1: Create Database Master Key (if it doesn't exist)
-- The master key is used to protect other keys and certificates in the database
IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
	CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'StrongP@ssw0rd!2024#SecureKey'
	PRINT 'Database Master Key created successfully'
END
ELSE
BEGIN
	PRINT 'Database Master Key already exists'
END
GO

-- Step 2: Create a Certificate (if it doesn't exist)
-- The certificate is used to protect the symmetric key
IF NOT EXISTS (SELECT * FROM sys.certificates WHERE name = 'UserPasswordCertificate')
BEGIN
	CREATE CERTIFICATE UserPasswordCertificate
	WITH SUBJECT = 'Certificate for User Password Encryption',
		 EXPIRY_DATE = '2099-12-31'
	PRINT 'Certificate created successfully'
END
ELSE
BEGIN
	PRINT 'Certificate already exists'
END
GO

-- Step 3: Create Symmetric Key (if it doesn't exist)
-- This is the key used for actual encryption/decryption
IF NOT EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = 'UserPasswordSymmetricKey')
BEGIN
	CREATE SYMMETRIC KEY UserPasswordSymmetricKey
	WITH ALGORITHM = AES_256
	ENCRYPTION BY CERTIFICATE UserPasswordCertificate
	PRINT 'Symmetric Key created successfully'
END
ELSE
BEGIN
	PRINT 'Symmetric Key already exists'
END
GO

-- Step 4: Verify the encryption infrastructure
PRINT '========================================='
PRINT 'Encryption Infrastructure Setup Complete'
PRINT '========================================='
PRINT 'Master Key: ' + CASE WHEN EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##') THEN 'Created' ELSE 'Not Found' END
PRINT 'Certificate: ' + CASE WHEN EXISTS (SELECT * FROM sys.certificates WHERE name = 'UserPasswordCertificate') THEN 'Created' ELSE 'Not Found' END
PRINT 'Symmetric Key: ' + CASE WHEN EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = 'UserPasswordSymmetricKey') THEN 'Created' ELSE 'Not Found' END
PRINT '========================================='
GO
