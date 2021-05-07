using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    AnnouncementID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(maxLength: 50, nullable: true),
                    OwnerID = table.Column<string>(nullable: true),
                    OwnerName = table.Column<string>(nullable: true),
                    Content = table.Column<string>(maxLength: 65536, nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    Views = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcement", x => x.AnnouncementID);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(maxLength: 50, nullable: true),
                    OwnerID = table.Column<string>(nullable: true),
                    OwnerName = table.Column<string>(nullable: true),
                    Content = table.Column<string>(maxLength: 65536, nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    Block = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.FeedbackID);
                });

            migrationBuilder.CreateTable(
                name: "Profile",
                columns: table => new
                {
                    ProfileID = table.Column<string>(nullable: false),
                    Introduction = table.Column<string>(maxLength: 256, nullable: true),
                    Image = table.Column<byte[]>(maxLength: 2097152, nullable: true),
                    RealCoins = table.Column<decimal>(type: "money", nullable: false),
                    VirtualCoins = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyCoins = table.Column<decimal>(type: "money", nullable: false),
                    UserName = table.Column<string>(maxLength: 50, nullable: false),
                    EnrollmentDate = table.Column<DateTime>(nullable: false),
                    LastTimeLogin = table.Column<DateTime>(nullable: false),
                    Professioal = table.Column<bool>(nullable: false),
                    Prime = table.Column<bool>(nullable: false),
                    Banned = table.Column<bool>(nullable: false),
                    Views = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile", x => x.ProfileID);
                });

            migrationBuilder.CreateTable(
                name: "Article",
                columns: table => new
                {
                    ArticleID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(maxLength: 50, nullable: true),
                    Content = table.Column<string>(maxLength: 65536, nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    Views = table.Column<int>(nullable: false),
                    Block = table.Column<bool>(nullable: false),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyCoins = table.Column<decimal>(type: "money", nullable: false),
                    ProfileID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Article", x => x.ArticleID);
                    table.ForeignKey(
                        name: "FK_Article_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    MissionsID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WatchArticle = table.Column<bool>(nullable: false),
                    Vote = table.Column<bool>(nullable: false),
                    LeaveComment = table.Column<bool>(nullable: false),
                    ViewAnnouncement = table.Column<bool>(nullable: false),
                    ShareCreation = table.Column<bool>(nullable: false),
                    BeAgreed = table.Column<bool>(nullable: false),
                    CompleteOtherMissions = table.Column<bool>(nullable: false),
                    ProfileID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.MissionsID);
                    table.ForeignKey(
                        name: "FK_Missions_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Novel",
                columns: table => new
                {
                    NovelID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(maxLength: 50, nullable: true),
                    Introduction = table.Column<string>(maxLength: 256, nullable: true),
                    Image = table.Column<byte[]>(maxLength: 2097152, nullable: true),
                    Type = table.Column<int>(nullable: true),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyCoins = table.Column<decimal>(type: "money", nullable: false),
                    PrimeRead = table.Column<bool>(nullable: false),
                    End = table.Column<bool>(nullable: false),
                    Views = table.Column<int>(nullable: false),
                    Block = table.Column<bool>(nullable: false),
                    ProfileID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Novel", x => x.NovelID);
                    table.ForeignKey(
                        name: "FK_Novel_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Episode",
                columns: table => new
                {
                    EpisodeID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(maxLength: 20, nullable: true),
                    OwnerID = table.Column<string>(nullable: true),
                    Content = table.Column<string>(maxLength: 65536, nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    Views = table.Column<int>(nullable: false),
                    HasVoting = table.Column<bool>(nullable: false),
                    IsLast = table.Column<bool>(nullable: false),
                    NovelID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Episode", x => x.EpisodeID);
                    table.ForeignKey(
                        name: "FK_Episode_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Follow",
                columns: table => new
                {
                    FollowID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FollowerID = table.Column<string>(nullable: true),
                    SubscribeUser = table.Column<bool>(nullable: false),
                    SubscribeNovel = table.Column<bool>(nullable: false),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    ProfileID = table.Column<string>(nullable: true),
                    NovelID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Follow", x => x.FollowID);
                    table.ForeignKey(
                        name: "FK_Follow_Profile_FollowerID",
                        column: x => x.FollowerID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Follow_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Follow_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Involving",
                columns: table => new
                {
                    InvolvingID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyValue = table.Column<decimal>(type: "money", nullable: false),
                    TotalValue = table.Column<decimal>(type: "money", nullable: false),
                    LastTime = table.Column<DateTime>(nullable: false),
                    ProfileID = table.Column<string>(nullable: true),
                    NovelID = table.Column<int>(nullable: true),
                    ArticleID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Involving", x => x.InvolvingID);
                    table.ForeignKey(
                        name: "FK_Involving_Article_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Involving_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Involving_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    CommentID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(maxLength: 16384, nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    Block = table.Column<bool>(nullable: false),
                    ProfileID = table.Column<string>(nullable: true),
                    NovelID = table.Column<int>(nullable: true),
                    EpisodeID = table.Column<int>(nullable: true),
                    AnnouncementID = table.Column<int>(nullable: true),
                    FeedbackID = table.Column<int>(nullable: true),
                    ArticleID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.CommentID);
                    table.ForeignKey(
                        name: "FK_Comment_Announcement_AnnouncementID",
                        column: x => x.AnnouncementID,
                        principalTable: "Announcement",
                        principalColumn: "AnnouncementID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comment_Article_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comment_Episode_EpisodeID",
                        column: x => x.EpisodeID,
                        principalTable: "Episode",
                        principalColumn: "EpisodeID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comment_Feedback_FeedbackID",
                        column: x => x.FeedbackID,
                        principalTable: "Feedback",
                        principalColumn: "FeedbackID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comment_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comment_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Voting",
                columns: table => new
                {
                    VotingID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(nullable: true),
                    Title = table.Column<string>(maxLength: 20, nullable: true),
                    Policy = table.Column<int>(nullable: false),
                    Mode = table.Column<int>(nullable: false),
                    Limit = table.Column<int>(nullable: false),
                    Threshold = table.Column<int>(nullable: false),
                    BiddingLowerLimit = table.Column<int>(nullable: true),
                    NumberLimit = table.Column<int>(nullable: true),
                    CoinLimit = table.Column<int>(nullable: true),
                    End = table.Column<bool>(nullable: false),
                    TotalNumber = table.Column<int>(nullable: false),
                    TotalCoins = table.Column<int>(nullable: false),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    DeadLine = table.Column<DateTime>(nullable: true),
                    EpisodeID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Voting", x => x.VotingID);
                    table.ForeignKey(
                        name: "FK_Voting_Episode_EpisodeID",
                        column: x => x.EpisodeID,
                        principalTable: "Episode",
                        principalColumn: "EpisodeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Dice",
                columns: table => new
                {
                    DiceID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sides = table.Column<int>(nullable: false),
                    Value = table.Column<int>(nullable: false),
                    CommentID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dice", x => x.DiceID);
                    table.ForeignKey(
                        name: "FK_Dice_Comment_CommentID",
                        column: x => x.CommentID,
                        principalTable: "Comment",
                        principalColumn: "CommentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Message",
                columns: table => new
                {
                    MessageID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(maxLength: 1024, nullable: true),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    Block = table.Column<bool>(nullable: false),
                    CommentID = table.Column<int>(nullable: false),
                    ProfileID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Message", x => x.MessageID);
                    table.ForeignKey(
                        name: "FK_Message_Comment_CommentID",
                        column: x => x.CommentID,
                        principalTable: "Comment",
                        principalColumn: "CommentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Message_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BiddingOption",
                columns: table => new
                {
                    BiddingOptionID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(nullable: true),
                    BiddingCoins = table.Column<decimal>(type: "money", nullable: false),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    CreateTime = table.Column<DateTime>(nullable: false),
                    Content = table.Column<string>(maxLength: 10, nullable: true),
                    VotingID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BiddingOption", x => x.BiddingOptionID);
                    table.ForeignKey(
                        name: "FK_BiddingOption_Voting_VotingID",
                        column: x => x.VotingID,
                        principalTable: "Voting",
                        principalColumn: "VotingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NormalOption",
                columns: table => new
                {
                    NormalOptionID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(nullable: true),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    Content = table.Column<string>(maxLength: 20, nullable: true),
                    VotingID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormalOption", x => x.NormalOptionID);
                    table.ForeignKey(
                        name: "FK_NormalOption_Voting_VotingID",
                        column: x => x.VotingID,
                        principalTable: "Voting",
                        principalColumn: "VotingID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Agree",
                columns: table => new
                {
                    AgreeID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UpdateTime = table.Column<DateTime>(nullable: false),
                    CommentID = table.Column<int>(nullable: true),
                    MessageID = table.Column<int>(nullable: true),
                    ProfileID = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agree", x => x.AgreeID);
                    table.ForeignKey(
                        name: "FK_Agree_Comment_CommentID",
                        column: x => x.CommentID,
                        principalTable: "Comment",
                        principalColumn: "CommentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agree_Message_MessageID",
                        column: x => x.MessageID,
                        principalTable: "Message",
                        principalColumn: "MessageID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Agree_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vote",
                columns: table => new
                {
                    VoteID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(nullable: true),
                    Value = table.Column<decimal>(type: "money", nullable: false),
                    BiddingOptionID = table.Column<int>(nullable: true),
                    NormalOptionID = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vote", x => x.VoteID);
                    table.ForeignKey(
                        name: "FK_Vote_BiddingOption_BiddingOptionID",
                        column: x => x.BiddingOptionID,
                        principalTable: "BiddingOption",
                        principalColumn: "BiddingOptionID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vote_NormalOption_NormalOptionID",
                        column: x => x.NormalOptionID,
                        principalTable: "NormalOption",
                        principalColumn: "NormalOptionID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Agree_CommentID",
                table: "Agree",
                column: "CommentID");

            migrationBuilder.CreateIndex(
                name: "IX_Agree_MessageID",
                table: "Agree",
                column: "MessageID");

            migrationBuilder.CreateIndex(
                name: "IX_Agree_ProfileID",
                table: "Agree",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Article_ProfileID",
                table: "Article",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_BiddingOption_VotingID",
                table: "BiddingOption",
                column: "VotingID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_AnnouncementID",
                table: "Comment",
                column: "AnnouncementID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ArticleID",
                table: "Comment",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_EpisodeID",
                table: "Comment",
                column: "EpisodeID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_FeedbackID",
                table: "Comment",
                column: "FeedbackID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_NovelID",
                table: "Comment",
                column: "NovelID");

            migrationBuilder.CreateIndex(
                name: "IX_Comment_ProfileID",
                table: "Comment",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Dice_CommentID",
                table: "Dice",
                column: "CommentID");

            migrationBuilder.CreateIndex(
                name: "IX_Episode_NovelID",
                table: "Episode",
                column: "NovelID");

            migrationBuilder.CreateIndex(
                name: "IX_Follow_FollowerID",
                table: "Follow",
                column: "FollowerID");

            migrationBuilder.CreateIndex(
                name: "IX_Follow_NovelID",
                table: "Follow",
                column: "NovelID");

            migrationBuilder.CreateIndex(
                name: "IX_Follow_ProfileID",
                table: "Follow",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Involving_ArticleID",
                table: "Involving",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_Involving_NovelID",
                table: "Involving",
                column: "NovelID");

            migrationBuilder.CreateIndex(
                name: "IX_Involving_ProfileID",
                table: "Involving",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Message_CommentID",
                table: "Message",
                column: "CommentID");

            migrationBuilder.CreateIndex(
                name: "IX_Message_ProfileID",
                table: "Message",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ProfileID",
                table: "Missions",
                column: "ProfileID",
                unique: true,
                filter: "[ProfileID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NormalOption_VotingID",
                table: "NormalOption",
                column: "VotingID");

            migrationBuilder.CreateIndex(
                name: "IX_Novel_ProfileID",
                table: "Novel",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_BiddingOptionID",
                table: "Vote",
                column: "BiddingOptionID");

            migrationBuilder.CreateIndex(
                name: "IX_Vote_NormalOptionID",
                table: "Vote",
                column: "NormalOptionID");

            migrationBuilder.CreateIndex(
                name: "IX_Voting_EpisodeID",
                table: "Voting",
                column: "EpisodeID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agree");

            migrationBuilder.DropTable(
                name: "Dice");

            migrationBuilder.DropTable(
                name: "Follow");

            migrationBuilder.DropTable(
                name: "Involving");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "Vote");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "BiddingOption");

            migrationBuilder.DropTable(
                name: "NormalOption");

            migrationBuilder.DropTable(
                name: "Comment");

            migrationBuilder.DropTable(
                name: "Voting");

            migrationBuilder.DropTable(
                name: "Announcement");

            migrationBuilder.DropTable(
                name: "Article");

            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "Episode");

            migrationBuilder.DropTable(
                name: "Novel");

            migrationBuilder.DropTable(
                name: "Profile");
        }
    }
}
