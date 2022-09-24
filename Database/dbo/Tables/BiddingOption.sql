CREATE TABLE [dbo].[BiddingOption] (
    [BiddingOptionID] INT            IDENTITY (1, 1) NOT NULL,
    [OwnerID]         NVARCHAR (MAX) NULL,
    [BiddingCoins]    MONEY          NOT NULL,
    [TotalCoins]      MONEY          NOT NULL,
    [CreateTime]      DATETIME2 (7)  NOT NULL,
    [Content]         NVARCHAR (10)  NULL,
    [VotingID]        INT            NOT NULL,
    CONSTRAINT [PK_BiddingOption] PRIMARY KEY CLUSTERED ([BiddingOptionID] ASC),
    CONSTRAINT [FK_BiddingOption_Voting_VotingID] FOREIGN KEY ([VotingID]) REFERENCES [dbo].[Voting] ([VotingID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_BiddingOption_VotingID]
    ON [dbo].[BiddingOption]([VotingID] ASC);

