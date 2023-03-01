CREATE TABLE [dbo].[Novel] (
    [NovelID]        INT             IDENTITY (1, 1) NOT NULL,
    [Title]          NVARCHAR (50)   NOT NULL,
    [Introduction]   NVARCHAR (512)  NOT NULL,
    [Type]           INT             NULL,
    [CreateTime]     DATETIME2 (7)   NOT NULL,
    [UpdateTime]     DATETIME2 (7)   NOT NULL,
    [TotalCoins]     MONEY           NOT NULL,
    [MonthlyCoins]   MONEY           NOT NULL,
    [PrimeRead]      BIT             NOT NULL,
    [End]            BIT             NOT NULL,
    [Views]          INT             NOT NULL,
    [Block]          BIT             NOT NULL,
    [ProfileID]      NVARCHAR (450)  NOT NULL,
    [ImageUrl]       NVARCHAR (1024) NULL,
    [DailyView]      INT             DEFAULT ((0)) NOT NULL,
    [ViewRecordJson] NVARCHAR (MAX)  NULL,
    CONSTRAINT [PK_Novel] PRIMARY KEY CLUSTERED ([NovelID] ASC),
    CONSTRAINT [FK_Novel_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE
);




GO
CREATE NONCLUSTERED INDEX [IX_Novel_ProfileID]
    ON [dbo].[Novel]([ProfileID] ASC);

