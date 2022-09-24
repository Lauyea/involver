CREATE TABLE [dbo].[NovelTags] (
    [TagId] INT           IDENTITY (1, 1) NOT NULL,
    [Name]  NVARCHAR (15) NOT NULL,
    CONSTRAINT [PK_NovelTags] PRIMARY KEY CLUSTERED ([TagId] ASC)
);

