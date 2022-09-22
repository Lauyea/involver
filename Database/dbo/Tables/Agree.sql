CREATE TABLE [dbo].[Agree] (
    [AgreeID]    INT            IDENTITY (1, 1) NOT NULL,
    [UpdateTime] DATETIME2 (7)  NOT NULL,
    [CommentID]  INT            NULL,
    [MessageID]  INT            NULL,
    [ProfileID]  NVARCHAR (450) NOT NULL,
    CONSTRAINT [PK_Agree] PRIMARY KEY CLUSTERED ([AgreeID] ASC),
    CONSTRAINT [FK_Agree_Comment_CommentID] FOREIGN KEY ([CommentID]) REFERENCES [dbo].[Comment] ([CommentID]),
    CONSTRAINT [FK_Agree_Message_MessageID] FOREIGN KEY ([MessageID]) REFERENCES [dbo].[Message] ([MessageID]),
    CONSTRAINT [FK_Agree_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Agree_CommentID]
    ON [dbo].[Agree]([CommentID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Agree_MessageID]
    ON [dbo].[Agree]([MessageID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Agree_ProfileID]
    ON [dbo].[Agree]([ProfileID] ASC);

