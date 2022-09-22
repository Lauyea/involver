CREATE TABLE [dbo].[Feedback] (
    [FeedbackID] INT            IDENTITY (1, 1) NOT NULL,
    [Title]      NVARCHAR (50)  NOT NULL,
    [OwnerID]    NVARCHAR (MAX) NULL,
    [OwnerName]  NVARCHAR (MAX) NULL,
    [Content]    NVARCHAR (MAX) NOT NULL,
    [UpdateTime] DATETIME2 (7)  NOT NULL,
    [Block]      BIT            NOT NULL,
    [Accept]     BIT            DEFAULT (CONVERT([bit],(0))) NOT NULL,
    CONSTRAINT [PK_Feedback] PRIMARY KEY CLUSTERED ([FeedbackID] ASC)
);

