CREATE TABLE [dbo].[Involving] (
    [InvolvingID]  INT            IDENTITY (1, 1) NOT NULL,
    [Value]        MONEY          NOT NULL,
    [MonthlyValue] MONEY          NOT NULL,
    [TotalValue]   MONEY          NOT NULL,
    [LastTime]     DATETIME2 (7)  NOT NULL,
    [ProfileID]    NVARCHAR (450) NULL,
    [NovelID]      INT            NULL,
    [ArticleID]    INT            NULL,
    [InvolverID]   NVARCHAR (450) NOT NULL,
    CONSTRAINT [PK_Involving] PRIMARY KEY CLUSTERED ([InvolvingID] ASC),
    CONSTRAINT [FK_Involving_Article_ArticleID] FOREIGN KEY ([ArticleID]) REFERENCES [dbo].[Article] ([ArticleID]),
    CONSTRAINT [FK_Involving_Novel_NovelID] FOREIGN KEY ([NovelID]) REFERENCES [dbo].[Novel] ([NovelID]),
    CONSTRAINT [FK_Involving_Profile_InvolverID] FOREIGN KEY ([InvolverID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Involving_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Involving_ArticleID]
    ON [dbo].[Involving]([ArticleID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Involving_InvolverID]
    ON [dbo].[Involving]([InvolverID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Involving_NovelID]
    ON [dbo].[Involving]([NovelID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Involving_ProfileID]
    ON [dbo].[Involving]([ProfileID] ASC);

