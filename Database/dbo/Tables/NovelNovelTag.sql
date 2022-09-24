CREATE TABLE [dbo].[NovelNovelTag] (
    [NovelTagsTagId] INT NOT NULL,
    [NovelsNovelID]  INT NOT NULL,
    CONSTRAINT [PK_NovelNovelTag] PRIMARY KEY CLUSTERED ([NovelTagsTagId] ASC, [NovelsNovelID] ASC),
    CONSTRAINT [FK_NovelNovelTag_Novel_NovelsNovelID] FOREIGN KEY ([NovelsNovelID]) REFERENCES [dbo].[Novel] ([NovelID]) ON DELETE CASCADE,
    CONSTRAINT [FK_NovelNovelTag_NovelTags_NovelTagsTagId] FOREIGN KEY ([NovelTagsTagId]) REFERENCES [dbo].[NovelTags] ([TagId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_NovelNovelTag_NovelsNovelID]
    ON [dbo].[NovelNovelTag]([NovelsNovelID] ASC);

