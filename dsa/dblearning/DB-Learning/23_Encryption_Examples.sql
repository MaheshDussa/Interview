/*
=====================================================================================
Script: Password Encryption Examples and Testing
Description: Demonstrates usage of symmetric key encryption for passwords
Author: System Generated
=====================================================================================
*/

PRINT '========================================='
PRINT 'PASSWORD ENCRYPTION EXAMPLES'
PRINT '========================================='
GO

-- Example 1: Encrypt a password using stored procedure
DECLARE @EncryptedPwd VARBINARY(256);
DECLARE @PlainPwd NVARCHAR(100) = 'MySecureP@ssw0rd!';

EXEC dbo.usp_EncryptPassword 
	@PlainPassword = @PlainPwd,
	@EncryptedPassword = @EncryptedPwd OUTPUT;

PRINT 'Example 1: Encrypt Password'
PRINT 'Plain Text: ' + @PlainPwd
PRINT 'Encrypted (Hex): ' + CONVERT(VARCHAR(MAX), @EncryptedPwd, 2)
PRINT ''
GO

-- Example 2: Decrypt a password using stored procedure
DECLARE @EncryptedPwd VARBINARY(256);
DECLARE @PlainPwd NVARCHAR(100) = 'MySecureP@ssw0rd!';
DECLARE @DecryptedPwd NVARCHAR(100);

-- First encrypt
EXEC dbo.usp_EncryptPassword 
	@PlainPassword = @PlainPwd,
	@EncryptedPassword = @EncryptedPwd OUTPUT;

-- Then decrypt
EXEC dbo.usp_DecryptPassword
	@EncryptedPassword = @EncryptedPwd,
	@DecryptedPassword = @DecryptedPwd OUTPUT;

PRINT 'Example 2: Decrypt Password'
PRINT 'Original: ' + @PlainPwd
PRINT 'Decrypted: ' + @DecryptedPwd
PRINT 'Match: ' + CASE WHEN @PlainPwd = @DecryptedPwd THEN 'YES' ELSE 'NO' END
PRINT ''
GO

-- Example 3: Create a user with encrypted password
DECLARE @NewUserId INT;

EXEC dbo.usp_CreateUser
	@FirstName = 'John',
	@LastName = 'Doe',
	@Email = 'john.doe@example.com',
	@Phone = '1234567890',
	@PlainPassword = 'JohnSecure123!',
	@IsActive = 1,
	@NewUserId = @NewUserId OUTPUT;

PRINT 'Example 3: Create User with Encrypted Password'
PRINT 'New User ID: ' + CAST(@NewUserId AS VARCHAR(10))
PRINT ''
GO

-- Example 4: Validate user password
DECLARE @IsValid BIT;

EXEC dbo.usp_ValidateUserPassword
	@Email = 'john.doe@example.com',
	@PlainPassword = 'JohnSecure123!',
	@IsValid = @IsValid OUTPUT;

PRINT 'Example 4: Validate User Password'
PRINT 'Email: john.doe@example.com'
PRINT 'Password Valid: ' + CASE WHEN @IsValid = 1 THEN 'YES' ELSE 'NO' END
PRINT ''
GO

-- Example 5: Validate with wrong password
DECLARE @IsValid BIT;

EXEC dbo.usp_ValidateUserPassword
	@Email = 'john.doe@example.com',
	@PlainPassword = 'WrongPassword',
	@IsValid = @IsValid OUTPUT;

PRINT 'Example 5: Validate with Wrong Password'
PRINT 'Email: john.doe@example.com'
PRINT 'Password Valid: ' + CASE WHEN @IsValid = 1 THEN 'YES' ELSE 'NO' END
PRINT ''
GO

-- Example 6: Direct encryption/decryption (inline method)
DECLARE @Password NVARCHAR(100) = 'DirectEncryption123!';
DECLARE @Encrypted VARBINARY(256);
DECLARE @Decrypted NVARCHAR(100);

-- Open the symmetric key
OPEN SYMMETRIC KEY UserPasswordSymmetricKey
DECRYPTION BY CERTIFICATE UserPasswordCertificate;

-- Encrypt
SET @Encrypted = EncryptByKey(Key_GUID('UserPasswordSymmetricKey'), @Password);

-- Decrypt
SET @Decrypted = CONVERT(NVARCHAR(100), DecryptByKey(@Encrypted));

-- Close the key
CLOSE SYMMETRIC KEY UserPasswordSymmetricKey;

PRINT 'Example 6: Direct Inline Encryption/Decryption'
PRINT 'Original: ' + @Password
PRINT 'Encrypted (Hex): ' + CONVERT(VARCHAR(MAX), @Encrypted, 2)
PRINT 'Decrypted: ' + @Decrypted
PRINT ''
GO

-- Example 7: View encrypted passwords (for demonstration only)
PRINT 'Example 7: View All Users with Encrypted Passwords'
PRINT '------------------------------------------------'
SELECT 
	UserId,
	FirstName,
	LastName,
	Email,
	CONVERT(VARCHAR(MAX), PasswordHash, 2) AS EncryptedPasswordHex,
	IsActive
FROM dbo.USERS
WHERE Email = 'john.doe@example.com'
GO

-- Example 8: Batch password update (encrypt existing plain text passwords)
-- This is useful for migrating existing plain text passwords to encrypted format
/*
DECLARE @UserId INT, @PlainPassword NVARCHAR(100), @EncryptedPassword VARBINARY(256);

-- Assuming you have a temporary column with plain text passwords
-- This example shows the pattern

DECLARE password_cursor CURSOR FOR
SELECT UserId, PlainPasswordColumn FROM USERS WHERE PasswordHash IS NULL;

OPEN password_cursor;
FETCH NEXT FROM password_cursor INTO @UserId, @PlainPassword;

WHILE @@FETCH_STATUS = 0
BEGIN
	EXEC dbo.usp_EncryptPassword 
		@PlainPassword = @PlainPassword,
		@EncryptedPassword = @EncryptedPassword OUTPUT;

	UPDATE USERS
	SET PasswordHash = @EncryptedPassword
	WHERE UserId = @UserId;

	FETCH NEXT FROM password_cursor INTO @UserId, @PlainPassword;
END

CLOSE password_cursor;
DEALLOCATE password_cursor;

PRINT 'Batch password encryption completed'
*/
GO

PRINT '========================================='
PRINT 'ALL EXAMPLES COMPLETED SUCCESSFULLY'
PRINT '========================================='
GO
