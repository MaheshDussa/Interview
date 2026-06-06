CREATE TABLE [dbo].[USERS] (
    [UserId]       INT            IDENTITY (1, 1) NOT NULL,
    [FirstName]    NVARCHAR (100) NULL,
    [LastName]     NVARCHAR (100) NULL,
    [Email]        NVARCHAR (100) NOT NULL,
    [Phone]        NVARCHAR (13)  NULL,
    [PasswordHash] VARBINARY (256) NULL,
    [IsActive]     BIT            DEFAULT ((0)) NULL,
    [CreatedDate]  DATETIME       DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [CHK_Email] CHECK ([Email] like '%@%.%'),
    UNIQUE NONCLUSTERED ([Email] ASC),
    UNIQUE NONCLUSTERED ([Phone] ASC)
);

