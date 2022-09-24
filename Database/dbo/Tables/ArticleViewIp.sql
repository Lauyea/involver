CREATE TABLE [dbo].[ArticleViewIp] (
    [ArticlesArticleID] INT NOT NULL,
    [ViewIpsId]         INT NOT NULL,
    CONSTRAINT [PK_ArticleViewIp] PRIMARY KEY CLUSTERED ([ArticlesArticleID] ASC, [ViewIpsId] ASC),
    CONSTRAINT [FK_ArticleViewIp_Article_ArticlesArticleID] FOREIGN KEY ([ArticlesArticleID]) REFERENCES [dbo].[Article] ([ArticleID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ArticleViewIp_ViewIps_ViewIpsId] FOREIGN KEY ([ViewIpsId]) REFERENCES [dbo].[ViewIps] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_ArticleViewIp_ViewIpsId]
    ON [dbo].[ArticleViewIp]([ViewIpsId] ASC);

