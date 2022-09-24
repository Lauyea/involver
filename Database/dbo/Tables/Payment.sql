CREATE TABLE [dbo].[Payment] (
    [PaymentID]       INT            IDENTITY (1, 1) NOT NULL,
    [RtnMsg]          NVARCHAR (MAX) NULL,
    [TradeNo]         NVARCHAR (MAX) NULL,
    [TradeAmt]        INT            NOT NULL,
    [PaymentDate]     NVARCHAR (MAX) NULL,
    [TradeDate]       NVARCHAR (MAX) NULL,
    [ReturnString]    NVARCHAR (MAX) NULL,
    [InvolverID]      NVARCHAR (MAX) NULL,
    [RequestBody]     NVARCHAR (MAX) NULL,
    [MerchantTradeNo] NVARCHAR (MAX) NULL,
    [RtnCode]         INT            DEFAULT ((0)) NOT NULL,
    [SimulatePaid]    INT            DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Payment] PRIMARY KEY CLUSTERED ([PaymentID] ASC)
);

