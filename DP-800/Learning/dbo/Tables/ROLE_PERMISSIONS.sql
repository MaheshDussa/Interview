CREATE TABLE [dbo].[ROLE_PERMISSIONS] (
    [PermissionId] INT           IDENTITY (1, 1) NOT NULL,
    [Name]         NVARCHAR (50) NOT NULL,
    PRIMARY KEY CLUSTERED ([PermissionId] ASC)
);

