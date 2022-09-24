CREATE TABLE [dbo].[Episode] (
    [EpisodeID]  INT            IDENTITY (1, 1) NOT NULL,
    [Title]      NVARCHAR (20)  NOT NULL,
    [OwnerID]    NVARCHAR (MAX) NULL,
    [Content]    NVARCHAR (MAX) NOT NULL,
    [UpdateTime] DATETIME2 (7)  NOT NULL,
    [Views]      INT            NOT NULL,
    [HasVoting]  BIT            NOT NULL,
    [IsLast]     BIT            NOT NULL,
    [NovelID]    INT            NOT NULL,
    CONSTRAINT [PK_Episode] PRIMARY KEY CLUSTERED ([EpisodeID] ASC),
    CONSTRAINT [FK_Episode_Novel_NovelID] FOREIGN KEY ([NovelID]) REFERENCES [dbo].[Novel] ([NovelID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Episode_NovelID]
    ON [dbo].[Episode]([NovelID] ASC);

