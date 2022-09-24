CREATE TABLE [dbo].[Announcement] (
    [AnnouncementID] INT            IDENTITY (1, 1) NOT NULL,
    [Title]          NVARCHAR (50)  NOT NULL,
    [OwnerID]        NVARCHAR (MAX) NULL,
    [OwnerName]      NVARCHAR (MAX) NULL,
    [Content]        NVARCHAR (MAX) NOT NULL,
    [UpdateTime]     DATETIME2 (7)  NOT NULL,
    [Views]          INT            NOT NULL,
    CONSTRAINT [PK_Announcement] PRIMARY KEY CLUSTERED ([AnnouncementID] ASC)
);

