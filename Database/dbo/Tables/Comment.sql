CREATE TABLE [dbo].[Comment] (
    [CommentID]      INT            IDENTITY (1, 1) NOT NULL,
    [Content]        NVARCHAR (MAX) NOT NULL,
    [UpdateTime]     DATETIME2 (7)  NOT NULL,
    [Block]          BIT            NOT NULL,
    [ProfileID]      NVARCHAR (450) NOT NULL,
    [NovelID]        INT            NULL,
    [EpisodeID]      INT            NULL,
    [AnnouncementID] INT            NULL,
    [FeedbackID]     INT            NULL,
    [ArticleID]      INT            NULL,
    CONSTRAINT [PK_Comment] PRIMARY KEY CLUSTERED ([CommentID] ASC),
    CONSTRAINT [FK_Comment_Announcement_AnnouncementID] FOREIGN KEY ([AnnouncementID]) REFERENCES [dbo].[Announcement] ([AnnouncementID]),
    CONSTRAINT [FK_Comment_Article_ArticleID] FOREIGN KEY ([ArticleID]) REFERENCES [dbo].[Article] ([ArticleID]),
    CONSTRAINT [FK_Comment_Episode_EpisodeID] FOREIGN KEY ([EpisodeID]) REFERENCES [dbo].[Episode] ([EpisodeID]),
    CONSTRAINT [FK_Comment_Feedback_FeedbackID] FOREIGN KEY ([FeedbackID]) REFERENCES [dbo].[Feedback] ([FeedbackID]),
    CONSTRAINT [FK_Comment_Novel_NovelID] FOREIGN KEY ([NovelID]) REFERENCES [dbo].[Novel] ([NovelID]),
    CONSTRAINT [FK_Comment_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Comment_AnnouncementID]
    ON [dbo].[Comment]([AnnouncementID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Comment_ArticleID]
    ON [dbo].[Comment]([ArticleID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Comment_EpisodeID]
    ON [dbo].[Comment]([EpisodeID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Comment_FeedbackID]
    ON [dbo].[Comment]([FeedbackID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Comment_NovelID]
    ON [dbo].[Comment]([NovelID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Comment_ProfileID]
    ON [dbo].[Comment]([ProfileID] ASC);

