CREATE TABLE [dbo].[NovelViewer] (
    [ProfileID] NVARCHAR (450) NOT NULL,
    [NovelID]   INT            NOT NULL,
    [SeqNo]     INT            IDENTITY (1, 1) NOT NULL,
    [ViewDate]  DATETIME2 (7)  DEFAULT (getdate()) NOT NULL,
    CONSTRAINT [PK_NovelViewer] PRIMARY KEY NONCLUSTERED ([ProfileID] ASC, [NovelID] ASC),
    CONSTRAINT [FK_NovelViewer_Novel_NovelID] FOREIGN KEY ([NovelID]) REFERENCES [dbo].[Novel] ([NovelID]) ON DELETE CASCADE,
    CONSTRAINT [FK_NovelViewer_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID])
);


GO
CREATE UNIQUE CLUSTERED INDEX [IX_NovelViewer_SeqNo]
    ON [dbo].[NovelViewer]([SeqNo] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_NovelViewer_NovelID]
    ON [dbo].[NovelViewer]([NovelID] ASC);

