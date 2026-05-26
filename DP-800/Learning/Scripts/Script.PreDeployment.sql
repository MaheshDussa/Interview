/*
	Pre-Deployment Script
	Purpose:    Creates security objects (Master Key, Certificate, Symmetric Key)
	Author:     Database Administrator

	This script executes before schema changes are deployed.
*/

SET NOCOUNT ON;

PRINT 'Starting Pre-Deployment Script...';

-- Include security setup scripts
:r "..\Security\Master Keys\CreateMasterKey.sql"
:r "..\Security\Certificates\DataEncryptionCert.sql"
:r "..\Security\Symmetric Keys\DataEncryptionKey.sql"

PRINT 'Pre-Deployment Script completed.';
GO