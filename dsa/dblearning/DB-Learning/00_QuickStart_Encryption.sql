/*
=====================================================================================
QUICK START GUIDE - PASSWORD ENCRYPTION WITH SYMMETRIC KEYS
=====================================================================================

STEP 1: DEPLOY THE DATABASE
----------------------------
The encryption infrastructure will be automatically set up during deployment through
the post-deployment script. It includes:
- Database Master Key
- Certificate for encryption
- Symmetric Key (AES-256)
- All stored procedures and functions

STEP 2: TEST THE IMPLEMENTATION
--------------------------------
Run this script to verify everything is working:
*/

-- Verify encryption objects exist
PRINT '=== VERIFICATION CHECK ==='
PRINT 'Master Key: ' + CASE WHEN EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##') THEN 'EXISTS' ELSE 'MISSING' END
PRINT 'Certificate: ' + CASE WHEN EXISTS (SELECT * FROM sys.certificates WHERE name = 'UserPasswordCertificate') THEN 'EXISTS' ELSE 'MISSING' END
PRINT 'Symmetric Key: ' + CASE WHEN EXISTS (SELECT * FROM sys.symmetric_keys WHERE name = 'UserPasswordSymmetricKey') THEN 'EXISTS' ELSE 'MISSING' END
GO

-- Test 1: Create a test user
DECLARE @UserId INT;
EXEC dbo.usp_CreateUser
	@FirstName = 'Test',
	@LastName = 'User',
	@Email = 'test.user@example.com',
	@Phone = '9876543210',
	@PlainPassword = 'TestP@ssw0rd123!',
	@IsActive = 1,
	@NewUserId = @UserId OUTPUT;

PRINT 'Test User Created with ID: ' + CAST(@UserId AS VARCHAR(10))
GO

-- Test 2: Validate the password (correct password)
DECLARE @IsValid BIT;
EXEC dbo.usp_ValidateUserPassword
	@Email = 'test.user@example.com',
	@PlainPassword = 'TestP@ssw0rd123!',
	@IsValid = @IsValid OUTPUT;

PRINT 'Password Validation (Correct): ' + CASE WHEN @IsValid = 1 THEN 'PASSED' ELSE 'FAILED' END
GO

-- Test 3: Validate with wrong password
DECLARE @IsValid BIT;
EXEC dbo.usp_ValidateUserPassword
	@Email = 'test.user@example.com',
	@PlainPassword = 'WrongPassword',
	@IsValid = @IsValid OUTPUT;

PRINT 'Password Validation (Wrong): ' + CASE WHEN @IsValid = 0 THEN 'PASSED' ELSE 'FAILED' END
GO

-- Test 4: Change password
DECLARE @Success BIT;
EXEC dbo.usp_ChangeUserPassword
	@Email = 'test.user@example.com',
	@OldPassword = 'TestP@ssw0rd123!',
	@NewPassword = 'NewTestP@ssw0rd456!',
	@Success = @Success OUTPUT;

PRINT 'Password Change: ' + CASE WHEN @Success = 1 THEN 'PASSED' ELSE 'FAILED' END
GO

-- Test 5: Validate with new password
DECLARE @IsValid BIT;
EXEC dbo.usp_ValidateUserPassword
	@Email = 'test.user@example.com',
	@PlainPassword = 'NewTestP@ssw0rd456!',
	@IsValid = @IsValid OUTPUT;

PRINT 'New Password Validation: ' + CASE WHEN @IsValid = 1 THEN 'PASSED' ELSE 'FAILED' END
GO

-- Test 6: View password info (metadata only, not the actual password)
EXEC dbo.usp_GetUserPasswordInfo @Email = 'test.user@example.com'
GO

PRINT ''
PRINT '=== ALL TESTS COMPLETED ==='
PRINT ''

/*
STEP 3: INTEGRATE WITH YOUR APPLICATION
----------------------------------------

C# Example (.NET):
------------------
*/

-- SQL Command from C# application:

-- CREATE USER
/*
using (SqlConnection conn = new SqlConnection(connectionString))
using (SqlCommand cmd = new SqlCommand("dbo.usp_CreateUser", conn))
{
	cmd.CommandType = CommandType.StoredProcedure;
	cmd.Parameters.AddWithValue("@FirstName", "John");
	cmd.Parameters.AddWithValue("@LastName", "Doe");
	cmd.Parameters.AddWithValue("@Email", "john.doe@example.com");
	cmd.Parameters.AddWithValue("@Phone", "1234567890");
	cmd.Parameters.AddWithValue("@PlainPassword", "SecureP@ss123");
	cmd.Parameters.AddWithValue("@IsActive", true);

	SqlParameter userIdParam = new SqlParameter("@NewUserId", SqlDbType.Int);
	userIdParam.Direction = ParameterDirection.Output;
	cmd.Parameters.Add(userIdParam);

	conn.Open();
	cmd.ExecuteNonQuery();

	int newUserId = (int)userIdParam.Value;
	Console.WriteLine($"User created with ID: {newUserId}");
}
*/

-- VALIDATE USER LOGIN
/*
using (SqlConnection conn = new SqlConnection(connectionString))
using (SqlCommand cmd = new SqlCommand("dbo.usp_ValidateUserPassword", conn))
{
	cmd.CommandType = CommandType.StoredProcedure;
	cmd.Parameters.AddWithValue("@Email", email);
	cmd.Parameters.AddWithValue("@PlainPassword", password);

	SqlParameter isValidParam = new SqlParameter("@IsValid", SqlDbType.Bit);
	isValidParam.Direction = ParameterDirection.Output;
	cmd.Parameters.Add(isValidParam);

	conn.Open();
	cmd.ExecuteNonQuery();

	bool isValid = (bool)isValidParam.Value;

	if (isValid)
		Console.WriteLine("Login successful");
	else
		Console.WriteLine("Invalid credentials");
}
*/

/*
Python Example:
---------------
*/

-- CREATE USER
/*
import pyodbc

conn = pyodbc.connect(connection_string)
cursor = conn.cursor()

cursor.execute("""
	DECLARE @UserId INT;
	EXEC dbo.usp_CreateUser 
		@FirstName=?, 
		@LastName=?, 
		@Email=?, 
		@PlainPassword=?, 
		@IsActive=1,
		@NewUserId=@UserId OUTPUT;
	SELECT @UserId;
""", 'John', 'Doe', 'john.doe@example.com', 'SecureP@ss123')

user_id = cursor.fetchone()[0]
print(f"User created with ID: {user_id}")
conn.commit()
*/

-- VALIDATE USER
/*
cursor.execute("""
	DECLARE @IsValid BIT;
	EXEC dbo.usp_ValidateUserPassword 
		@Email=?, 
		@PlainPassword=?, 
		@IsValid=@IsValid OUTPUT;
	SELECT @IsValid;
""", email, password)

is_valid = cursor.fetchone()[0]

if is_valid:
	print("Login successful")
else:
	print("Invalid credentials")
*/

/*
STEP 4: SECURITY CHECKLIST
---------------------------

□ Change the master key password in 21_Setup_Encryption_Keys.sql
□ Backup the database master key
□ Backup the certificate and private key
□ Restrict access to decryption procedures
□ Implement password complexity rules in application
□ Never log or display decrypted passwords
□ Set up monitoring for failed login attempts
□ Document recovery procedures
□ Schedule regular key rotation (if required by policy)
□ Test disaster recovery procedures

STEP 5: BACKUP COMMANDS (RUN AFTER DEPLOYMENT)
-----------------------------------------------
*/

-- Backup Master Key
/*
BACKUP MASTER KEY TO FILE = 'C:\Backup\LEARNING_MasterKey.key'
ENCRYPTION BY PASSWORD = 'YourStrongBackupP@ssw0rd!';
*/

-- Backup Certificate
/*
BACKUP CERTIFICATE UserPasswordCertificate 
TO FILE = 'C:\Backup\UserPasswordCert.cer'
WITH PRIVATE KEY (
	FILE = 'C:\Backup\UserPasswordCert.pvk',
	ENCRYPTION BY PASSWORD = 'YourStrongCertBackupP@ssw0rd!'
);
*/

/*
STEP 6: COMMON OPERATIONS
--------------------------

Check all stored procedures:
*/
SELECT 
	SCHEMA_NAME(schema_id) + '.' + name AS ProcedureName,
	create_date,
	modify_date
FROM sys.procedures
WHERE name LIKE '%Password%'
ORDER BY name;

-- Check encryption objects:
SELECT 'Master Key' AS ObjectType, @@SERVERNAME AS Location
FROM sys.symmetric_keys 
WHERE name = '##MS_DatabaseMasterKey##'

UNION ALL

SELECT 'Certificate', name
FROM sys.certificates 
WHERE name = 'UserPasswordCertificate'

UNION ALL

SELECT 'Symmetric Key', name
FROM sys.symmetric_keys 
WHERE name = 'UserPasswordSymmetricKey';

/*
TROUBLESHOOTING:
----------------

If encryption doesn't work after database restore:

1. Check if master key needs password reset:
   ALTER MASTER KEY REGENERATE WITH ENCRYPTION BY PASSWORD = 'NewP@ssw0rd!';

2. Verify certificate is accessible:
   SELECT * FROM sys.certificates WHERE name = 'UserPasswordCertificate';

3. Test encryption/decryption manually:
   Run the examples in 23_Encryption_Examples.sql

SUPPORT:
--------
For detailed documentation, see: Scripts/README_Encryption.sql
For examples, see: Scripts/23_Encryption_Examples.sql

=====================================================================================
*/

PRINT 'Quick Start Guide Loaded - Follow steps above to implement password encryption'
GO
