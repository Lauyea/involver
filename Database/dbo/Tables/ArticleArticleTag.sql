CREATE TABLE [dbo].[ArticleArticleTag] (
    [ArticleTagsTagId]  INT NOT NULL,
    [ArticlesArticleID] INT NOT NULL,
    CONSTRAINT [PK_ArticleArticleTag] PRIMARY KEY CLUSTERED ([ArticleTagsTagId] ASC, [ArticlesArticleID] ASC),
    CONSTRAINT [FK_ArticleArticleTag_Article_ArticlesArticleID] FOREIGN KEY ([ArticlesArticleID]) REFERENCES [dbo].[Article] ([ArticleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ArticleArticleTag_ArticleTags_ArticleTagsTagId] FOREIGN KEY ([ArticleTagsTagId]) REFERENCES [dbo].[ArticleTags] ([TagId]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_ArticleArticleTag_ArticlesArticleID]
    ON [dbo].[ArticleArticleTag]([ArticlesArticleID] ASC);

