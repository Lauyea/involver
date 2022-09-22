CREATE TABLE [dbo].[ProfileAchievement] (
    [ProfileID]     NVARCHAR (450) NOT NULL,
    [AchievementID] INT            NOT NULL,
    [AchieveDate]   DATETIME2 (7)  DEFAULT (getdate()) NOT NULL,
    [SeqNo]         INT            IDENTITY (1, 1) NOT NULL,
    CONSTRAINT [PK_ProfileAchievement] PRIMARY KEY NONCLUSTERED ([ProfileID] ASC, [AchievementID] ASC),
    CONSTRAINT [FK_ProfileAchievement_Achievement_AchievementID] FOREIGN KEY ([AchievementID]) REFERENCES [dbo].[Achievements] ([AchievementID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ProfileAchievement_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE
);


GO
CREATE UNIQUE CLUSTERED INDEX [IX_ProfileAchievement_SeqNo]
    ON [dbo].[ProfileAchievement]([SeqNo] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ProfileAchievement_AchievementID]
    ON [dbo].[ProfileAchievement]([AchievementID] ASC);

