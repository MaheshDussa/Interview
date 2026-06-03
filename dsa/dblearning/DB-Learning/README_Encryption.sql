/*
=====================================================================================
SYMMETRIC KEY PASSWORD ENCRYPTION - DOCUMENTATION
=====================================================================================

OVERVIEW:
This encryption solution uses SQL Server's built-in symmetric key encryption to 
securely store user passwords. It implements AES-256 encryption with a certificate-
protected symmetric key.

ENCRYPTION HIERARCHY:
1. Database Master Key - Encrypted by service master key and password
2. Certificate - Protected by database master key
3. Symmetric Key (AES-256) - Protected by certificate
4. User Passwords - Encrypted by symmetric key

=====================================================================================
FILES INCLUDED:
=====================================================================================

1. 21_Setup_Encryption_Keys.sql
   - Creates database master key
   - Creates certificate for encryption
   - Creates symmetric key with AES-256 algorithm
   - Verification checks

2. 22_Encryption_StoredProcedures.sql
   - usp_EncryptPassword: Encrypts a plain text password
   - usp_DecryptPassword: Decrypts an encrypted password
   - usp_CreateUser: Creates user with encrypted password
   - usp_ValidateUserPassword: Validates user login credentials

3. 23_Encryption_Examples.sql
   - 8 practical examples demonstrating usage
   - Testing and verification scripts
   - Sample data insertion

4. 24_Encryption_Utilities.sql
   - fn_DecryptPasswordInline: Function for inline decryption
   - usp_ChangeUserPassword: Change password with validation
   - usp_ResetUserPassword: Admin password reset
   - usp_GetUserPasswordInfo: Get password metadata

=====================================================================================
INSTALLATION INSTRUCTIONS:
=====================================================================================

Step 1: Execute scripts in order
   a. 21_Setup_Encryption_Keys.sql
   b. 22_Encryption_StoredProcedures.sql
   c. 24_Encryption_Utilities.sql

Step 2: Test the implementation
   - Run 23_Encryption_Examples.sql to verify everything works

Step 3: Update deployment scripts
   - Add encryption setup to post-deployment script

=====================================================================================
USAGE EXAMPLES:
=====================================================================================

-- CREATE A NEW USER WITH ENCRYPTED PASSWORD
DECLARE @UserId INT;
EXEC dbo.usp_CreateUser
	@FirstName = 'Jane',
	@LastName = 'Smith',
	@Email = 'jane.smith@example.com',
	@PlainPassword = 'SecureP@ss123',
	@IsActive = 1,
	@NewUserId = @UserId OUTPUT;

-- VALIDATE USER LOGIN
DECLARE @IsValid BIT;
EXEC dbo.usp_ValidateUserPassword
	@Email = 'jane.smith@example.com',
	@PlainPassword = 'SecureP@ss123',
	@IsValid = @IsValid OUTPUT;

IF @IsValid = 1
	PRINT 'Login successful';
ELSE
	PRINT 'Invalid credentials';

-- CHANGE PASSWORD
DECLARE @Success BIT;
EXEC dbo.usp_ChangeUserPassword
	@Email = 'jane.smith@example.com',
	@OldPassword = 'SecureP@ss123',
	@NewPassword = 'NewSecureP@ss456',
	@Success = @Success OUTPUT;

-- ADMIN PASSWORD RESET
DECLARE @Success BIT;
EXEC dbo.usp_ResetUserPassword
	@Email = 'jane.smith@example.com',
	@NewPassword = 'ResetP@ssword789',
	@Success = @Success OUTPUT;

-- ENCRYPT PASSWORD MANUALLY
DECLARE @Encrypted VARBINARY(256);
EXEC dbo.usp_EncryptPassword
	@PlainPassword = 'MyPassword',
	@EncryptedPassword = @Encrypted OUTPUT;

-- DECRYPT PASSWORD MANUALLY
DECLARE @Decrypted NVARCHAR(100);
EXEC dbo.usp_DecryptPassword
	@EncryptedPassword = @Encrypted,
	@DecryptedPassword = @Decrypted OUTPUT;

=====================================================================================
SECURITY BEST PRACTICES:
=====================================================================================

1. MASTER KEY PASSWORD:
   - Change the default password in 21_Setup_Encryption_Keys.sql
   - Use a strong, complex password
   - Store the password securely (password manager, key vault)
   - Document the password in secure location for disaster recovery

2. CERTIFICATE BACKUP:
   - Backup the certificate and private key:
	 BACKUP CERTIFICATE UserPasswordCertificate 
	 TO FILE = 'C:\Backup\UserPasswordCert.cer'
	 WITH PRIVATE KEY (
		 FILE = 'C:\Backup\UserPasswordCert.pvk',
		 ENCRYPTION BY PASSWORD = 'StrongBackupP@ssw0rd!'
	 );

3. DATABASE MASTER KEY BACKUP:
   - Backup the master key:
	 BACKUP MASTER KEY TO FILE = 'C:\Backup\MasterKey.key'
	 ENCRYPTION BY PASSWORD = 'BackupP@ssw0rd!';

4. ACCESS CONTROL:
   - Grant EXECUTE permissions only to necessary roles
   - Restrict access to decryption procedures
   - Never expose decrypted passwords in application logs

5. APPLICATION INTEGRATION:
   - Applications should only use usp_CreateUser and usp_ValidateUserPassword
   - Never retrieve or display decrypted passwords
   - Implement password complexity rules in application layer

6. MONITORING:
   - Audit failed login attempts
   - Monitor usage of decryption procedures
   - Set up alerts for suspicious activity

=====================================================================================
PERFORMANCE CONSIDERATIONS:
=====================================================================================

1. Key Opening/Closing:
   - Opening and closing keys has overhead
   - Stored procedures handle this automatically
   - For bulk operations, consider opening key once at session level

2. Indexing:
   - Encrypted columns cannot be efficiently indexed
   - Use Email (not password) for user lookups
   - Encryption adds minimal overhead to single-row operations

3. Bulk Operations:
   - For bulk password updates, use session-level key opening
   - See Example 8 in 23_Encryption_Examples.sql

=====================================================================================
DISASTER RECOVERY:
=====================================================================================

To restore encryption on a new server:

1. Restore database backup
2. Restore master key:
   RESTORE MASTER KEY FROM FILE = 'C:\Backup\MasterKey.key'
   DECRYPTION BY PASSWORD = 'BackupP@ssw0rd!'
   ENCRYPTION BY PASSWORD = 'NewServerP@ssw0rd!';

3. Restore certificate (if needed):
   CREATE CERTIFICATE UserPasswordCertificate
   FROM FILE = 'C:\Backup\UserPasswordCert.cer'
   WITH PRIVATE KEY (
	   FILE = 'C:\Backup\UserPasswordCert.pvk',
	   DECRYPTION BY PASSWORD = 'StrongBackupP@ssw0rd!'
   );

4. Verify encryption works:
   - Run validation queries from 23_Encryption_Examples.sql

=====================================================================================
TROUBLESHOOTING:
=====================================================================================

ERROR: "Cannot find the symmetric key"
SOLUTION: Ensure the key exists and you have permissions:
   SELECT * FROM sys.symmetric_keys WHERE name = 'UserPasswordSymmetricKey'

ERROR: "The key is not open"
SOLUTION: The stored procedures handle this automatically. If using manual
   encryption, ensure you OPEN the key before use and CLOSE after.

ERROR: "Cannot decrypt or encrypt using the specified key"
SOLUTION: Verify the certificate exists and is valid:
   SELECT * FROM sys.certificates WHERE name = 'UserPasswordCertificate'

ERROR: Decryption returns NULL
SOLUTION: The data may have been encrypted with a different key, or the
   certificate/master key may have been restored incorrectly.

=====================================================================================
MIGRATION FROM PLAIN TEXT:
=====================================================================================

If you have existing plain text passwords, use this pattern:

1. Add a temporary column for plain text (if needed)
2. Run batch encryption script (see Example 8 in 23_Encryption_Examples.sql)
3. Verify all passwords are encrypted
4. Drop the temporary plain text column

=====================================================================================
COMPARISON: SYMMETRIC KEY vs HASHING
=====================================================================================

SYMMETRIC KEY ENCRYPTION (This Solution):
✓ Can decrypt and retrieve original password
✓ Useful for password reset/recovery scenarios
✓ Can integrate with external systems requiring plain passwords
✗ If keys are compromised, all passwords are at risk
✗ Requires secure key management

PASSWORD HASHING (Alternative):
✓ Cannot be decrypted - more secure
✓ Industry standard for password storage
✓ No key management needed
✗ Cannot retrieve original password
✗ Password reset requires user to create new password

RECOMMENDATION:
- For most applications: Use HASHBYTES with salt (more secure)
- For legacy systems requiring reversible encryption: Use this solution
- Never store passwords in plain text

=====================================================================================
CONVERTING TO HASH-BASED AUTHENTICATION:
=====================================================================================

For better security, consider migrating to hash-based authentication:

ALTER TABLE USERS ADD PasswordSalt VARBINARY(16);
ALTER TABLE USERS ADD PasswordHashSHA256 VARBINARY(32);

CREATE PROCEDURE usp_CreateUserWithHash
	@Email NVARCHAR(100),
	@PlainPassword NVARCHAR(100)
AS
BEGIN
	DECLARE @Salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
	DECLARE @Hash VARBINARY(32);

	SET @Hash = HASHBYTES('SHA2_256', @PlainPassword + CONVERT(NVARCHAR(100), @Salt));

	INSERT INTO USERS (Email, PasswordSalt, PasswordHashSHA256)
	VALUES (@Email, @Salt, @Hash);
END

=====================================================================================
SUPPORT AND MAINTENANCE:
=====================================================================================

Regular Maintenance Tasks:
1. Monitor certificate expiry dates (expires 2099-12-31)
2. Regular backups of master key and certificate
3. Periodic password rotation policies
4. Security audits of encryption usage

For Issues:
- Check SQL Server error logs
- Verify encryption hierarchy is intact
- Test with examples in 23_Encryption_Examples.sql
- Review security permissions on encryption objects

=====================================================================================
*/
