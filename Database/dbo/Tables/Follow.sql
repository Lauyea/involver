CREATE TABLE [dbo].[Follow] (
    [FollowID]               INT            IDENTITY (1, 1) NOT NULL,
    [FollowerID]             NVARCHAR (450) NOT NULL,
    [ProfileMonthlyInvolver] BIT            NOT NULL,
    [NovelMonthlyInvolver]   BIT            NOT NULL,
    [UpdateTime]             DATETIME2 (7)  NOT NULL,
    [ProfileID]              NVARCHAR (450) NULL,
    [NovelID]                INT            NULL,
    CONSTRAINT [PK_Follow] PRIMARY KEY CLUSTERED ([FollowID] ASC),
    CONSTRAINT [FK_Follow_Novel_NovelID] FOREIGN KEY ([NovelID]) REFERENCES [dbo].[Novel] ([NovelID]),
    CONSTRAINT [FK_Follow_Profile_FollowerID] FOREIGN KEY ([FollowerID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Follow_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Follow_FollowerID]
    ON [dbo].[Follow]([FollowerID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Follow_NovelID]
    ON [dbo].[Follow]([NovelID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Follow_ProfileID]
    ON [dbo].[Follow]([ProfileID] ASC);

