/*
	Script:     CreateDefaultRoles.sql
	Purpose:    Creates default roles and assigns to default user if needed
	Author:     Database Administrator
*/

PRINT 'Checking for existing roles...';

IF NOT EXISTS (SELECT 1 FROM dbo.ROLES)
BEGIN
	PRINT 'No roles found. Creating default roles...';

	INSERT INTO dbo.ROLES 
	(
		[RoleName]
	)
	VALUES 
		(N'Administrator'),
		(N'User'),
		(N'ReadOnly');

	PRINT 'Default roles created successfully.';
END
ELSE
BEGIN
	PRINT 'Roles already exist in the database. Skipping default roles creation.';
END
GO

-- Assign Administrator role to default user if not already assigned
PRINT 'Checking default user role assignment...';

IF EXISTS (SELECT 1 FROM dbo.USERS WHERE [Email] = N'admin@system.local')
   AND EXISTS (SELECT 1 FROM dbo.ROLES WHERE [RoleName] = N'Administrator')
   AND NOT EXISTS (
	   SELECT 1 
	   FROM dbo.USER_ROLES ur
	   INNER JOIN dbo.USERS u ON ur.UserId = u.UserId
	   INNER JOIN dbo.ROLES r ON ur.RoleId = r.RoleId
	   WHERE u.[Email] = N'admin@system.local' 
		 AND r.[RoleName] = N'Administrator'
   )
BEGIN
	PRINT 'Assigning Administrator role to default user...';

	INSERT INTO dbo.USER_ROLES ([UserId], [RoleId])
	SELECT u.UserId, r.RoleId
	FROM dbo.USERS u
	CROSS JOIN dbo.ROLES r
	WHERE u.[Email] = N'admin@system.local'
	  AND r.[RoleName] = N'Administrator';

	PRINT 'Administrator role assigned to default user.';
END
GO
