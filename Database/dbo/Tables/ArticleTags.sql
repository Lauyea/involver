CREATE TABLE [dbo].[ArticleTags] (
    [TagId] INT           IDENTITY (1, 1) NOT NULL,
    [Name]  NVARCHAR (15) NOT NULL,
    CONSTRAINT [PK_ArticleTags] PRIMARY KEY CLUSTERED ([TagId] ASC)
);

