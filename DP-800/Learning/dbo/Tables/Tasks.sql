CREATE TABLE [dbo].[Tasks] (
    [TaskId]      INT            IDENTITY (1, 1) NOT NULL,
    [UserId]      INT            NOT NULL,
    [Title]       NVARCHAR (150) NOT NULL,
    [IsCompleted] BIT            DEFAULT ((0)) NULL,
    [DueDate]     DATETIME       NULL,
    [CreatedAt]   DATETIME       DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([TaskId] ASC),
    FOREIGN KEY ([UserId]) REFERENCES [dbo].[USERS] ([UserId]) ON DELETE CASCADE
);

