CREATE TABLE [dbo].[Voting] (
    [VotingID]          INT            IDENTITY (1, 1) NOT NULL,
    [OwnerID]           NVARCHAR (MAX) NULL,
    [Title]             NVARCHAR (20)  NOT NULL,
    [Policy]            INT            NOT NULL,
    [Mode]              INT            NOT NULL,
    [Limit]             INT            NOT NULL,
    [Threshold]         INT            NOT NULL,
    [BiddingLowerLimit] INT            NULL,
    [NumberLimit]       INT            NULL,
    [CoinLimit]         INT            NULL,
    [End]               BIT            NOT NULL,
    [TotalNumber]       INT            NOT NULL,
    [TotalCoins]        INT            NOT NULL,
    [CreateTime]        DATETIME2 (7)  NOT NULL,
    [DeadLine]          DATETIME2 (7)  NULL,
    [EpisodeID]         INT            NOT NULL,
    CONSTRAINT [PK_Voting] PRIMARY KEY CLUSTERED ([VotingID] ASC),
    CONSTRAINT [FK_Voting_Episode_EpisodeID] FOREIGN KEY ([EpisodeID]) REFERENCES [dbo].[Episode] ([EpisodeID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Voting_EpisodeID]
    ON [dbo].[Voting]([EpisodeID] ASC);

