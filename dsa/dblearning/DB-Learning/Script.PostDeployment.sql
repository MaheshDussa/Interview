/*
 Post-Deployment Script Template                            
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.        
 Use SQLCMD syntax to include a file in the post-deployment script.            
 Example:      :r .\myfile.sql                                
 Use SQLCMD syntax to reference a variable in the post-deployment script.        
 Example:      :setvar TableName MyTable                            
			   SELECT * FROM [$(TableName)]                    
--------------------------------------------------------------------------------------
*/

PRINT 'Starting Post-Deployment Script'
PRINT 'Date: ' + CONVERT(VARCHAR(50), GETDATE(), 120)
GO

-- Re-enable triggers if they were disabled (example)
-- Uncomment if triggers were disabled in pre-deployment
/*
ENABLE TRIGGER ALL ON DATABASE
PRINT 'All triggers enabled'
GO
*/

-- Seed reference data for ROLES table (example)
-- This uses MERGE to ensure idempotent deployments
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ROLES')
BEGIN
	MERGE INTO ROLES AS Target
	USING (VALUES
		(1, 'Administrator', 'System administrator with full access'),
		(2, 'User', 'Standard user with limited access'),
		(3, 'ReadOnly', 'Read-only access to data')
	) AS Source (RoleId, RoleName, Description)
	ON Target.RoleId = Source.RoleId
	WHEN MATCHED THEN
		UPDATE SET 
			RoleName = Source.RoleName,
			Description = Source.Description
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (RoleId, RoleName, Description)
		VALUES (Source.RoleId, Source.RoleName, Source.Description);

	PRINT 'Reference data seeded for ROLES table'
END
GO

-- Seed reference data for ROLE_PERMISSIONS table (example)
-- Uncomment and modify based on your permission structure
/*
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ROLE_PERMISSIONS')
BEGIN
	MERGE INTO ROLE_PERMISSIONS AS Target
	USING (VALUES
		(1, 'Read'),
		(2, 'Write'),
		(3, 'Delete'),
		(4, 'Admin')
	) AS Source (PermissionId, PermissionName)
	ON Target.PermissionId = Source.PermissionId
	WHEN MATCHED THEN
		UPDATE SET PermissionName = Source.PermissionName
	WHEN NOT MATCHED BY TARGET THEN
		INSERT (PermissionId, PermissionName)
		VALUES (Source.PermissionId, Source.PermissionName);

	PRINT 'Reference data seeded for ROLE_PERMISSIONS table'
END
GO
*/

-- Setup encryption infrastructure (symmetric keys for password encryption)
PRINT '----------------------------------------'
PRINT 'Setting up encryption infrastructure...'
:r .\21_Setup_Encryption_Keys.sql
:r .\22_Encryption_StoredProcedures.sql
:r .\24_Encryption_Utilities.sql
PRINT 'Encryption infrastructure setup completed'
PRINT '----------------------------------------'
GO

-- Update statistics for optimal query performance
EXEC sp_updatestats
PRINT 'Database statistics updated'
GO

-- Log the completion of deployment
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DeploymentLog')
BEGIN
	INSERT INTO DeploymentLog (DeploymentType, DatabaseName, Status, Notes)
	VALUES ('Post-Deployment', DB_NAME(), 'Completed', 'Post-deployment script execution completed successfully')
	PRINT 'Deployment logged successfully'
END
GO

-- Verification checks (example)
PRINT '----------------------------------------'
PRINT 'Deployment Verification:'
PRINT 'Total Tables: ' + CAST((SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0) AS VARCHAR(10))
PRINT 'Total Stored Procedures: ' + CAST((SELECT COUNT(*) FROM sys.procedures WHERE is_ms_shipped = 0) AS VARCHAR(10))
PRINT 'Total Views: ' + CAST((SELECT COUNT(*) FROM sys.views WHERE is_ms_shipped = 0) AS VARCHAR(10))
PRINT 'Total Functions: ' + CAST((SELECT COUNT(*) FROM sys.objects WHERE type IN ('FN', 'IF', 'TF') AND is_ms_shipped = 0) AS VARCHAR(10))
PRINT 'Encryption Objects: ' + CAST((SELECT COUNT(*) FROM sys.symmetric_keys WHERE name = 'UserPasswordSymmetricKey') + 
                                      (SELECT COUNT(*) FROM sys.certificates WHERE name = 'UserPasswordCertificate') AS VARCHAR(10))
PRINT '----------------------------------------'
GO

PRINT 'Post-Deployment Script Completed Successfully'
GO
