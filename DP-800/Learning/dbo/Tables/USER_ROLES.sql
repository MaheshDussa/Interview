CREATE TABLE [dbo].[USER_ROLES] (
    [UserId] INT NOT NULL,
    [RoleId] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleId] ASC),
    FOREIGN KEY ([RoleId]) REFERENCES [dbo].[ROLES] ([RoleId]),
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[USERS] ([UserId])
);

