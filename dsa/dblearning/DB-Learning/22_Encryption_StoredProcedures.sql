/*
=====================================================================================
Stored Procedure: usp_EncryptPassword
Description: Encrypts a plain text password using symmetric key encryption
Parameters: @PlainPassword - The password to encrypt
Returns: Encrypted password as VARBINARY
Author: System Generated
=====================================================================================
*/

IF OBJECT_ID('dbo.usp_EncryptPassword', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_EncryptPassword
GO

CREATE PROCEDURE dbo.usp_EncryptPassword
	@PlainPassword NVARCHAR(100),
	@EncryptedPassword VARBINARY(256) OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRY
		-- Open the symmetric key
		OPEN SYMMETRIC KEY UserPasswordSymmetricKey
		DECRYPTION BY CERTIFICATE UserPasswordCertificate;

		-- Encrypt the password
		SET @EncryptedPassword = EncryptByKey(Key_GUID('UserPasswordSymmetricKey'), @PlainPassword);

		-- Close the symmetric key
		CLOSE SYMMETRIC KEY UserPasswordSymmetricKey;

	END TRY
	BEGIN CATCH
		-- Close key if still open
		IF EXISTS (SELECT * FROM sys.openkeys WHERE key_name = 'UserPasswordSymmetricKey')
			CLOSE SYMMETRIC KEY UserPasswordSymmetricKey;

		-- Rethrow the error
		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
		DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
		DECLARE @ErrorState INT = ERROR_STATE();

		RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
	END CATCH
END
GO

PRINT 'Stored Procedure usp_EncryptPassword created successfully'
GO

/*
=====================================================================================
Stored Procedure: usp_DecryptPassword
Description: Decrypts an encrypted password using symmetric key
Parameters: @EncryptedPassword - The encrypted password
Returns: Decrypted password as NVARCHAR
Author: System Generated
=====================================================================================
*/

IF OBJECT_ID('dbo.usp_DecryptPassword', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_DecryptPassword
GO

CREATE PROCEDURE dbo.usp_DecryptPassword
	@EncryptedPassword VARBINARY(256),
	@DecryptedPassword NVARCHAR(100) OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	BEGIN TRY
		-- Open the symmetric key
		OPEN SYMMETRIC KEY UserPasswordSymmetricKey
		DECRYPTION BY CERTIFICATE UserPasswordCertificate;

		-- Decrypt the password
		SET @DecryptedPassword = CONVERT(NVARCHAR(100), DecryptByKey(@EncryptedPassword));

		-- Close the symmetric key
		CLOSE SYMMETRIC KEY UserPasswordSymmetricKey;

	END TRY
	BEGIN CATCH
		-- Close key if still open
		IF EXISTS (SELECT * FROM sys.openkeys WHERE key_name = 'UserPasswordSymmetricKey')
			CLOSE SYMMETRIC KEY UserPasswordSymmetricKey;

		-- Rethrow the error
		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
		DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
		DECLARE @ErrorState INT = ERROR_STATE();

		RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
	END CATCH
END
GO

PRINT 'Stored Procedure usp_DecryptPassword created successfully'
GO

/*
=====================================================================================
Stored Procedure: usp_CreateUser
Description: Creates a new user with encrypted password
Parameters: 
	@FirstName - User's first name
	@LastName - User's last name
	@Email - User's email (unique)
	@Phone - User's phone number
	@PlainPassword - Plain text password (will be encrypted)
	@IsActive - Active status
Returns: UserId of newly created user
Author: System Generated
=====================================================================================
*/

IF OBJECT_ID('dbo.usp_CreateUser', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_CreateUser
GO

CREATE PROCEDURE dbo.usp_CreateUser
	@FirstName NVARCHAR(100),
	@LastName NVARCHAR(100),
	@Email NVARCHAR(100),
	@Phone NVARCHAR(13) = NULL,
	@PlainPassword NVARCHAR(100),
	@IsActive BIT = 1,
	@NewUserId INT OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @EncryptedPassword VARBINARY(256);

	BEGIN TRY
		BEGIN TRANSACTION;

		-- Encrypt the password
		EXEC dbo.usp_EncryptPassword 
			@PlainPassword = @PlainPassword, 
			@EncryptedPassword = @EncryptedPassword OUTPUT;

		-- Insert the new user
		INSERT INTO dbo.USERS (FirstName, LastName, Email, Phone, PasswordHash, IsActive)
		VALUES (@FirstName, @LastName, @Email, @Phone, @EncryptedPassword, @IsActive);

		-- Get the new user ID
		SET @NewUserId = SCOPE_IDENTITY();

		COMMIT TRANSACTION;

		PRINT 'User created successfully with UserId: ' + CAST(@NewUserId AS VARCHAR(10));

	END TRY
	BEGIN CATCH
		IF @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;

		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
		DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
		DECLARE @ErrorState INT = ERROR_STATE();

		RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
	END CATCH
END
GO

PRINT 'Stored Procedure usp_CreateUser created successfully'
GO

/*
=====================================================================================
Stored Procedure: usp_ValidateUserPassword
Description: Validates a user's password by comparing plain text with encrypted value
Parameters: 
	@Email - User's email
	@PlainPassword - Plain text password to validate
Returns: 1 if valid, 0 if invalid
Author: System Generated
=====================================================================================
*/

IF OBJECT_ID('dbo.usp_ValidateUserPassword', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_ValidateUserPassword
GO

CREATE PROCEDURE dbo.usp_ValidateUserPassword
	@Email NVARCHAR(100),
	@PlainPassword NVARCHAR(100),
	@IsValid BIT OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @StoredEncryptedPassword VARBINARY(256);
	DECLARE @DecryptedPassword NVARCHAR(100);

	BEGIN TRY
		-- Get the encrypted password from database
		SELECT @StoredEncryptedPassword = PasswordHash
		FROM dbo.USERS
		WHERE Email = @Email AND IsActive = 1;

		IF @StoredEncryptedPassword IS NULL
		BEGIN
			SET @IsValid = 0;
			RETURN;
		END

		-- Decrypt the stored password
		EXEC dbo.usp_DecryptPassword 
			@EncryptedPassword = @StoredEncryptedPassword,
			@DecryptedPassword = @DecryptedPassword OUTPUT;

		-- Compare passwords
		IF @DecryptedPassword = @PlainPassword
			SET @IsValid = 1;
		ELSE
			SET @IsValid = 0;

	END TRY
	BEGIN CATCH
		SET @IsValid = 0;

		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
		PRINT 'Error validating password: ' + @ErrorMessage;
	END CATCH
END
GO

PRINT 'Stored Procedure usp_ValidateUserPassword created successfully'
GO
