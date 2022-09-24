CREATE TABLE [dbo].[Profile] (
    [ProfileID]         NVARCHAR (450) NOT NULL,
    [Introduction]      NVARCHAR (256) NULL,
    [RealCoins]         MONEY          NOT NULL,
    [VirtualCoins]      MONEY          NOT NULL,
    [MonthlyCoins]      MONEY          NOT NULL,
    [UserName]          NVARCHAR (50)  NOT NULL,
    [EnrollmentDate]    DATETIME2 (7)  NOT NULL,
    [LastTimeLogin]     DATETIME2 (7)  NOT NULL,
    [Professional]      BIT            NOT NULL,
    [Prime]             BIT            NOT NULL,
    [Banned]            BIT            NOT NULL,
    [Views]             INT            NOT NULL,
    [CanChangeUserName] BIT            DEFAULT (CONVERT([bit],(0))) NOT NULL,
    [BannerImageUrl]    NVARCHAR (128) NULL,
    [ImageUrl]          NVARCHAR (128) NULL,
    [SeqNo]             INT            IDENTITY (1, 1) NOT NULL,
    [UsedCoins]         MONEY          DEFAULT ((0.0)) NOT NULL,
    CONSTRAINT [PK_Profile] PRIMARY KEY NONCLUSTERED ([ProfileID] ASC)
);


GO
CREATE UNIQUE CLUSTERED INDEX [IX_Profile_SeqNo]
    ON [dbo].[Profile]([SeqNo] ASC);

