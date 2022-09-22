CREATE TABLE [dbo].[NormalOption] (
    [NormalOptionID] INT            IDENTITY (1, 1) NOT NULL,
    [OwnerID]        NVARCHAR (MAX) NULL,
    [TotalCoins]     MONEY          NOT NULL,
    [Content]        NVARCHAR (20)  NULL,
    [VotingID]       INT            NOT NULL,
    CONSTRAINT [PK_NormalOption] PRIMARY KEY CLUSTERED ([NormalOptionID] ASC),
    CONSTRAINT [FK_NormalOption_Voting_VotingID] FOREIGN KEY ([VotingID]) REFERENCES [dbo].[Voting] ([VotingID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_NormalOption_VotingID]
    ON [dbo].[NormalOption]([VotingID] ASC);

