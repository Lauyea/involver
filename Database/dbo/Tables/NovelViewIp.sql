CREATE TABLE [dbo].[NovelViewIp] (
    [NovelsNovelID] INT NOT NULL,
    [ViewIpsId]     INT NOT NULL,
    CONSTRAINT [PK_NovelViewIp] PRIMARY KEY CLUSTERED ([NovelsNovelID] ASC, [ViewIpsId] ASC),
    CONSTRAINT [FK_NovelViewIp_Novel_NovelsNovelID] FOREIGN KEY ([NovelsNovelID]) REFERENCES [dbo].[Novel] ([NovelID]) ON DELETE CASCADE,
    CONSTRAINT [FK_NovelViewIp_ViewIps_ViewIpsId] FOREIGN KEY ([ViewIpsId]) REFERENCES [dbo].[ViewIps] ([Id]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_NovelViewIp_ViewIpsId]
    ON [dbo].[NovelViewIp]([ViewIpsId] ASC);

