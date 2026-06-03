/*
=====================================================================================
Script: Utility Functions for Password Encryption
Description: Additional utility functions for password management
Author: System Generated
=====================================================================================
*/

/*
Function: fn_DecryptPasswordInline
Description: Table-valued function to decrypt passwords in SELECT queries
Usage: SELECT UserId, Email, dbo.fn_DecryptPasswordInline(PasswordHash) AS Password FROM USERS
Note: For administrative use only - not recommended for regular application use
*/

IF OBJECT_ID('dbo.fn_DecryptPasswordInline', 'FN') IS NOT NULL
	DROP FUNCTION dbo.fn_DecryptPasswordInline
GO

CREATE FUNCTION dbo.fn_DecryptPasswordInline(@EncryptedPassword VARBINARY(256))
RETURNS NVARCHAR(100)
AS
BEGIN
	DECLARE @DecryptedPassword NVARCHAR(100);

	-- Note: This requires the key to be opened at the session level
	-- Use with caution and only for administrative purposes
	SET @DecryptedPassword = CONVERT(NVARCHAR(100), DecryptByKey(@EncryptedPassword));

	RETURN @DecryptedPassword;
END
GO

PRINT 'Function fn_DecryptPasswordInline created successfully'
GO

/*
=====================================================================================
Stored Procedure: usp_ChangeUserPassword
Description: Changes a user's password after validating the old password
Parameters: 
	@Email - User's email
	@OldPassword - Current password (for validation)
	@NewPassword - New password to set
Returns: Success/Failure message
Author: System Generated
=====================================================================================
*/

IF OBJECT_ID('dbo.usp_ChangeUserPassword', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_ChangeUserPassword
GO

CREATE PROCEDURE dbo.usp_ChangeUserPassword
	@Email NVARCHAR(100),
	@OldPassword NVARCHAR(100),
	@NewPassword NVARCHAR(100),
	@Success BIT OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @IsValid BIT;
	DECLARE @NewEncryptedPassword VARBINARY(256);

	BEGIN TRY
		BEGIN TRANSACTION;

		-- Validate the old password
		EXEC dbo.usp_ValidateUserPassword
			@Email = @Email,
			@PlainPassword = @OldPassword,
			@IsValid = @IsValid OUTPUT;

		IF @IsValid = 0
		BEGIN
			SET @Success = 0;
			PRINT 'Password change failed: Invalid current password';
			ROLLBACK TRANSACTION;
			RETURN;
		END

		-- Encrypt the new password
		EXEC dbo.usp_EncryptPassword
			@PlainPassword = @NewPassword,
			@EncryptedPassword = @NewEncryptedPassword OUTPUT;

		-- Update the password
		UPDATE dbo.USERS
		SET PasswordHash = @NewEncryptedPassword
		WHERE Email = @Email;

		SET @Success = 1;
		COMMIT TRANSACTION;

		PRINT 'Password changed successfully for: ' + @Email;

	END TRY
	BEGIN CATCH
		IF @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;

		SET @Success = 0;

		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
		PRINT 'Error changing password: ' + @ErrorMessage;

		RAISERROR(@ErrorMessage, 16, 1);
	END CATCH
END
GO

PRINT 'Stored Procedure usp_ChangeUserPassword created successfully'
GO

/*
=====================================================================================
Stored Procedure: usp_ResetUserPassword
Description: Resets a user's password (admin function - no validation required)
Parameters: 
	@Email - User's email
	@NewPassword - New password to set
Returns: Success/Failure message
Author: System Generated
=====================================================================================
*/

IF OBJECT_ID('dbo.usp_ResetUserPassword', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_ResetUserPassword
GO

CREATE PROCEDURE dbo.usp_ResetUserPassword
	@Email NVARCHAR(100),
	@NewPassword NVARCHAR(100),
	@Success BIT OUTPUT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @NewEncryptedPassword VARBINARY(256);
	DECLARE @UserId INT;

	BEGIN TRY
		BEGIN TRANSACTION;

		-- Check if user exists
		SELECT @UserId = UserId
		FROM dbo.USERS
		WHERE Email = @Email;

		IF @UserId IS NULL
		BEGIN
			SET @Success = 0;
			PRINT 'Password reset failed: User not found';
			ROLLBACK TRANSACTION;
			RETURN;
		END

		-- Encrypt the new password
		EXEC dbo.usp_EncryptPassword
			@PlainPassword = @NewPassword,
			@EncryptedPassword = @NewEncryptedPassword OUTPUT;

		-- Update the password
		UPDATE dbo.USERS
		SET PasswordHash = @NewEncryptedPassword
		WHERE UserId = @UserId;

		SET @Success = 1;
		COMMIT TRANSACTION;

		PRINT 'Password reset successfully for: ' + @Email;

	END TRY
	BEGIN CATCH
		IF @@TRANCOUNT > 0
			ROLLBACK TRANSACTION;

		SET @Success = 0;

		DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
		PRINT 'Error resetting password: ' + @ErrorMessage;

		RAISERROR(@ErrorMessage, 16, 1);
	END CATCH
END
GO

PRINT 'Stored Procedure usp_ResetUserPassword created successfully'
GO

/*
=====================================================================================
Stored Procedure: usp_GetUserPasswordInfo
Description: Gets information about password encryption (for debugging/admin)
Parameters: @Email - User's email
Returns: Password metadata (not the actual password)
Author: System Generated
=====================================================================================
*/

IF OBJECT_ID('dbo.usp_GetUserPasswordInfo', 'P') IS NOT NULL
	DROP PROCEDURE dbo.usp_GetUserPasswordInfo
GO

CREATE PROCEDURE dbo.usp_GetUserPasswordInfo
	@Email NVARCHAR(100)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		UserId,
		Email,
		CASE 
			WHEN PasswordHash IS NULL THEN 'No Password Set'
			ELSE 'Password Encrypted'
		END AS PasswordStatus,
		DATALENGTH(PasswordHash) AS EncryptedPasswordLength,
		CONVERT(VARCHAR(50), PasswordHash, 2) AS EncryptedPasswordHex,
		CreatedDate,
		IsActive
	FROM dbo.USERS
	WHERE Email = @Email;
END
GO

PRINT 'Stored Procedure usp_GetUserPasswordInfo created successfully'
GO

PRINT '========================================='
PRINT 'All utility functions created successfully'
PRINT '========================================='
GO
