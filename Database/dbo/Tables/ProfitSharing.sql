CREATE TABLE [dbo].[ProfitSharing] (
    [ProfitSharingID] INT            IDENTITY (1, 1) NOT NULL,
    [InvolverID]      NVARCHAR (MAX) NULL,
    [CreditCard]      NVARCHAR (MAX) NULL,
    [SharingValue]    INT            NOT NULL,
    [SharingDone]     BIT            NOT NULL,
    CONSTRAINT [PK_ProfitSharing] PRIMARY KEY CLUSTERED ([ProfitSharingID] ASC)
);

