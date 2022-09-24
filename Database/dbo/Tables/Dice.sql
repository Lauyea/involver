CREATE TABLE [dbo].[Dice] (
    [DiceID]    INT IDENTITY (1, 1) NOT NULL,
    [Sides]     INT NOT NULL,
    [Value]     INT NOT NULL,
    [CommentID] INT NOT NULL,
    CONSTRAINT [PK_Dice] PRIMARY KEY CLUSTERED ([DiceID] ASC),
    CONSTRAINT [FK_Dice_Comment_CommentID] FOREIGN KEY ([CommentID]) REFERENCES [dbo].[Comment] ([CommentID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Dice_CommentID]
    ON [dbo].[Dice]([CommentID] ASC);

