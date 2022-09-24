CREATE TABLE [dbo].[ViewIps] (
    [Id] INT           IDENTITY (1, 1) NOT NULL,
    [Ip] NVARCHAR (20) NOT NULL,
    CONSTRAINT [PK_ViewIps] PRIMARY KEY CLUSTERED ([Id] ASC)
);

