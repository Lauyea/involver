CREATE TABLE [dbo].[Article] (
    [ArticleID]      INT             IDENTITY (1, 1) NOT NULL,
    [Title]          NVARCHAR (50)   NOT NULL,
    [Content]        NVARCHAR (MAX)  NOT NULL,
    [UpdateTime]     DATETIME2 (7)   NOT NULL,
    [Views]          INT             NOT NULL,
    [Block]          BIT             NOT NULL,
    [TotalCoins]     MONEY           NOT NULL,
    [MonthlyCoins]   MONEY           NOT NULL,
    [ProfileID]      NVARCHAR (450)  NOT NULL,
    [CreateTime]     DATETIME2 (7)   DEFAULT (getdate()) NOT NULL,
    [DailyView]      INT             DEFAULT ((0)) NOT NULL,
    [ViewRecordJson] NVARCHAR (MAX)  NULL,
    [ImageUrl]       NVARCHAR (1024) NULL,
    CONSTRAINT [PK_Article] PRIMARY KEY CLUSTERED ([ArticleID] ASC),
    CONSTRAINT [FK_Article_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE
);






GO
CREATE NONCLUSTERED INDEX [IX_Article_ProfileID]
    ON [dbo].[Article]([ProfileID] ASC);

