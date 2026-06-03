/*
Post-Deployment Script - Insert Default User with Encrypted Password
--------------------------------------------------------------------------------------
This script inserts a default admin user if it doesn't already exist.
The password is encrypted using the symmetric key DataEncryptionKey.
Default credentials:
  Email: admin@example.com
  Password: Admin@123
--------------------------------------------------------------------------------------
*/

-- Check if the default user already exists
IF NOT EXISTS (SELECT 1 FROM [dbo].[USERS] WHERE [Email] = 'admin@example.com')
BEGIN
    -- Declare variables for the password encryption
    DECLARE @PlainTextPassword NVARCHAR(100) = 'Admin@123';
    DECLARE @EncryptedPassword VARBINARY(256);

    -- Open the symmetric key for encryption
    OPEN SYMMETRIC KEY [DataEncryptionKey]
    DECRYPTION BY CERTIFICATE [DataEncryptionCert];

    -- Encrypt the password
    SET @EncryptedPassword = ENCRYPTBYKEY(KEY_GUID('DataEncryptionKey'), @PlainTextPassword);

    -- Close the symmetric key
    CLOSE SYMMETRIC KEY [DataEncryptionKey];

    -- Insert the default user with encrypted password
    INSERT INTO [dbo].[USERS] 
    (
        [FirstName],
        [LastName],
        [Email],
        [Phone],
        [PasswordHash],
        [IsActive],
        [CreatedDate]
    )
    VALUES 
    (
        'Admin',
        'User',
        'admin@example.com',
        '1234567890',
        @EncryptedPassword,
        1,
        GETDATE()
    );

    PRINT 'Default admin user created successfully with encrypted password.';
END
ELSE
BEGIN
    PRINT 'Default admin user already exists. Skipping insertion.';
END
GO
