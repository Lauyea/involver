CREATE TABLE [dbo].[Missions] (
    [MissionsID]            INT            IDENTITY (1, 1) NOT NULL,
    [WatchArticle]          BIT            NOT NULL,
    [Vote]                  BIT            NOT NULL,
    [LeaveComment]          BIT            NOT NULL,
    [ViewAnnouncement]      BIT            NOT NULL,
    [ShareCreation]         BIT            NOT NULL,
    [BeAgreed]              BIT            NOT NULL,
    [CompleteOtherMissions] BIT            NOT NULL,
    [ProfileID]             NVARCHAR (450) NOT NULL,
    [DailyLogin]            BIT            DEFAULT (CONVERT([bit],(0))) NOT NULL,
    CONSTRAINT [PK_Missions] PRIMARY KEY CLUSTERED ([MissionsID] ASC),
    CONSTRAINT [FK_Missions_Profile_ProfileID] FOREIGN KEY ([ProfileID]) REFERENCES [dbo].[Profile] ([ProfileID]) ON DELETE CASCADE
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_Missions_ProfileID]
    ON [dbo].[Missions]([ProfileID] ASC);

