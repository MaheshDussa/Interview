/*
 Pre-Deployment Script Template                            
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be executed before the build script.    
 Use SQLCMD syntax to include a file in the pre-deployment script.            
 Example:      :r .\myfile.sql                                
 Use SQLCMD syntax to reference a variable in the pre-deployment script.        
 Example:      :setvar TableName MyTable                            
			   SELECT * FROM [$(TableName)]                    
--------------------------------------------------------------------------------------
*/

-- Print deployment information
PRINT 'Starting Pre-Deployment Script'
PRINT 'Database: $(DatabaseName)'
PRINT 'Date: ' + CONVERT(VARCHAR(50), GETDATE(), 120)
GO

-- Create deployment log table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DeploymentLog')
BEGIN
	CREATE TABLE DeploymentLog (
		DeploymentId INT IDENTITY(1,1) PRIMARY KEY,
		DeploymentType NVARCHAR(50) NOT NULL,
		DeploymentDate DATETIME2 NOT NULL DEFAULT GETDATE(),
		DatabaseName NVARCHAR(128) NOT NULL,
		DeployedBy NVARCHAR(128) NOT NULL DEFAULT SYSTEM_USER,
		Status NVARCHAR(50) NOT NULL,
		Notes NVARCHAR(MAX) NULL
	)
	PRINT 'DeploymentLog table created'
END
GO

-- Log the start of deployment
INSERT INTO DeploymentLog (DeploymentType, DatabaseName, Status, Notes)
VALUES ('Pre-Deployment', DB_NAME(), 'Started', 'Pre-deployment script execution started')
GO

-- Backup critical data before deployment (example)
-- Uncomment and modify as needed for your specific requirements
/*
IF OBJECT_ID('tempdb..#UserBackup') IS NOT NULL
	DROP TABLE #UserBackup

SELECT * INTO #UserBackup FROM USERS
PRINT 'User data backed up to temporary table'
GO
*/

-- Disable triggers if needed during deployment (example)
-- Uncomment if you need to disable triggers
/*
DISABLE TRIGGER ALL ON DATABASE
PRINT 'All triggers disabled'
GO
*/

PRINT 'Pre-Deployment Script Completed Successfully'
GO
