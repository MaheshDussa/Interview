/*
	Post-Deployment Script
	Purpose:    Creates default user and roles if none exist
	Author:     Database Administrator

	This script executes after schema changes are deployed.
*/

SET NOCOUNT ON;



-- Include default data scripts
:r ".\PostDeployment\Data\CreateDefaultUser.sql"
:r ".\PostDeployment\Data\CreateDefaultRoles.sql"
GO
