CREATE TABLE [dbo].[Categories] (
    [CategoryId]   INT          IDENTITY (1, 1) NOT NULL,
    [UserId]       INT          NOT NULL,
    [CategoryName] VARCHAR (50) NOT NULL,
    [ColorHex]     VARCHAR (7)  DEFAULT ('#FFFFFF') NULL,
    PRIMARY KEY CLUSTERED ([CategoryId] ASC),
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[USERS] ([UserId]) ON DELETE CASCADE
);

