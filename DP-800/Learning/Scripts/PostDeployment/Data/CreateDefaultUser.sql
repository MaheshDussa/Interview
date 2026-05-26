/*
	Script:     CreateDefaultUser.sql
	Purpose:    Creates a default system user if no users exist in the database
	Author:     Database Administrator
*/

PRINT 'Checking for existing users...';

IF NOT EXISTS (SELECT 1 FROM dbo.USERS)
BEGIN
	PRINT 'No users found. Creating default system user...';

	INSERT INTO dbo.USERS 
	(
		[FirstName],
		[LastName],
		[Email],
		[IsActive],
		[CreatedDate]
	)
	VALUES 
	(
		N'System',
		N'Administrator',
		N'admin@system.local',
		1,
		GETUTCDATE()
	);

	PRINT 'Default system user created successfully.';
END
ELSE
BEGIN
	PRINT 'Users already exist in the database. Skipping default user creation.';
END
GO
