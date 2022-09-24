CREATE TABLE [dbo].[Vote] (
    [VoteID]          INT            IDENTITY (1, 1) NOT NULL,
    [OwnerID]         NVARCHAR (MAX) NULL,
    [Value]           MONEY          NOT NULL,
    [BiddingOptionID] INT            NULL,
    [NormalOptionID]  INT            NULL,
    CONSTRAINT [PK_Vote] PRIMARY KEY CLUSTERED ([VoteID] ASC),
    CONSTRAINT [FK_Vote_BiddingOption_BiddingOptionID] FOREIGN KEY ([BiddingOptionID]) REFERENCES [dbo].[BiddingOption] ([BiddingOptionID]),
    CONSTRAINT [FK_Vote_NormalOption_NormalOptionID] FOREIGN KEY ([NormalOptionID]) REFERENCES [dbo].[NormalOption] ([NormalOptionID])
);


GO
CREATE NONCLUSTERED INDEX [IX_Vote_BiddingOptionID]
    ON [dbo].[Vote]([BiddingOptionID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Vote_NormalOptionID]
    ON [dbo].[Vote]([NormalOptionID] ASC);

