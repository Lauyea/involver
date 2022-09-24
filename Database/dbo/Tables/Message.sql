CREATE TABLE [dbo].[Message] (
    [MessageID]  INT             IDENTITY (1, 1) NOT NULL,
    [Content]    NVARCHAR (1024) NOT NULL,
    [UpdateTime] DATETIME2 (7)   NOT NULL,
    [Block]      BIT             NOT NULL,
    [CommentID]  INT             NOT NULL,
    [ProfileID]  NVARCHAR (450)  NULL,
    CONSTRAINT [PK_Message] PRIMARY KEY CLUSTERED ([MessageID] ASC),
    CONSTRAINT [FK_Message_Comment_CommentID] FOREIGN KEY ([CommentID]) REFERENCES [dbo].[Comment] ([CommentID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Message_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Message_CommentID]
    ON [dbo].[Message]([CommentID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Message_ProfileID]
    ON [dbo].[Message]([ProfileID] ASC);

