CREATE TABLE [dbo].[Notifications] (
    [Id]          INT            IDENTITY (1, 1) NOT NULL,
    [Title]       NVARCHAR (MAX) NULL,
    [CreatedDate] DATETIME2 (7)  NOT NULL,
    [IsRead]      BIT            NOT NULL,
    [Url]         NVARCHAR (128) NULL,
    [ProfileID]   NVARCHAR (450) NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Notifications_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Notifications_ProfileID]
    ON [dbo].[Notifications]([ProfileID] ASC);

