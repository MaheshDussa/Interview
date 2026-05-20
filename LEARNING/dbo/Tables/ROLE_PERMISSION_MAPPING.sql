CREATE TABLE [dbo].[ROLE_PERMISSION_MAPPING] (
    [RoleId]       INT NOT NULL,
    [PermissionId] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([RoleId] ASC, [PermissionId] ASC),
    FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[ROLE_PERMISSIONS] ([PermissionId]),
    FOREIGN KEY ([RoleId]) REFERENCES [dbo].[ROLES] ([RoleId])
);

