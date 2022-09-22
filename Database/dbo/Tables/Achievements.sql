CREATE TABLE [dbo].[Achievements] (
    [AchievementID] INT            IDENTITY (1, 1) NOT NULL,
    [Title]         NVARCHAR (32)  NOT NULL,
    [Content]       NVARCHAR (128) NULL,
    [Rank]          INT            DEFAULT ((0)) NOT NULL,
    [Code]          NVARCHAR (32)  DEFAULT (N'') NOT NULL,
    CONSTRAINT [PK_Achievements] PRIMARY KEY CLUSTERED ([AchievementID] ASC)
);

