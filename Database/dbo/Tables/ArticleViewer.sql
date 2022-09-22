CREATE TABLE [dbo].[ArticleViewer] (
    [ProfileID] NVARCHAR (450) NOT NULL,
    [ArticleID] INT            NOT NULL,
    [SeqNo]     INT            IDENTITY (1, 1) NOT NULL,
    [ViewDate]  DATETIME2 (7)  DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_ArticleViewer] PRIMARY KEY NONCLUSTERED ([ProfileID] ASC, [ArticleID] ASC),
    CONSTRAINT [FK_ArticleViewer_Article_ArticleID] FOREIGN KEY ([ArticleID]) REFERENCES [dbo].[Article] ([ArticleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ArticleViewer_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID])
);


GO
CREATE UNIQUE CLUSTERED INDEX [IX_ArticleViewer_SeqNo]
    ON [dbo].[ArticleViewer]([SeqNo] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ArticleViewer_ArticleID]
    ON [dbo].[ArticleViewer]([ArticleID] ASC);

