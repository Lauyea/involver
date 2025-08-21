using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RenameTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agree_Comment_CommentID",
                table: "Agree");

            migrationBuilder.DropForeignKey(
                name: "FK_Agree_Message_MessageID",
                table: "Agree");

            migrationBuilder.DropForeignKey(
                name: "FK_Agree_Profile_ProfileID",
                table: "Agree");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Profile_ProfileID",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleViewer_Profile_ProfileID",
                table: "ArticleViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_BiddingOption_Voting_VotingID",
                table: "BiddingOption");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Announcement_AnnouncementID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Articles_ArticleID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Episode_EpisodeID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Feedback_FeedbackID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Novel_NovelID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Comment_Profile_ProfileID",
                table: "Comment");

            migrationBuilder.DropForeignKey(
                name: "FK_Dice_Comment_CommentID",
                table: "Dice");

            migrationBuilder.DropForeignKey(
                name: "FK_Episode_Novel_NovelID",
                table: "Episode");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Novel_NovelID",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Profile_FollowerID",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Follow_Profile_ProfileID",
                table: "Follow");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Articles_ArticleID",
                table: "Involving");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Novel_NovelID",
                table: "Involving");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving");

            migrationBuilder.DropForeignKey(
                name: "FK_Involving_Profile_ProfileID",
                table: "Involving");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_Comment_CommentID",
                table: "Message");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_Profile_ProfileID",
                table: "Message");

            migrationBuilder.DropForeignKey(
                name: "FK_Missions_Profile_ProfileID",
                table: "Missions");

            migrationBuilder.DropForeignKey(
                name: "FK_NormalOption_Voting_VotingID",
                table: "NormalOption");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Profile_ProfileID",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Novel_Profile_ProfileID",
                table: "Novel");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelNovelTag_Novel_NovelsNovelID",
                table: "NovelNovelTag");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelViewer_Novel_NovelID",
                table: "NovelViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelViewer_Profile_ProfileID",
                table: "NovelViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelViewIp_Novel_NovelsNovelID",
                table: "NovelViewIp");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileAchievement_Profile_ProfileID",
                table: "ProfileAchievement");

            migrationBuilder.DropForeignKey(
                name: "FK_Vote_BiddingOption_BiddingOptionID",
                table: "Vote");

            migrationBuilder.DropForeignKey(
                name: "FK_Vote_NormalOption_NormalOptionID",
                table: "Vote");

            migrationBuilder.DropForeignKey(
                name: "FK_Voting_Episode_EpisodeID",
                table: "Voting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Voting",
                table: "Voting");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Vote",
                table: "Vote");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfitSharing",
                table: "ProfitSharing");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profile",
                table: "Profile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payment",
                table: "Payment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Novel",
                table: "Novel");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NormalOption",
                table: "NormalOption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Message",
                table: "Message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Involving",
                table: "Involving");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Follow",
                table: "Follow");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Feedback",
                table: "Feedback");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Episode",
                table: "Episode");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Dice",
                table: "Dice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comment",
                table: "Comment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BiddingOption",
                table: "BiddingOption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Announcement",
                table: "Announcement");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Agree",
                table: "Agree");

            migrationBuilder.RenameTable(
                name: "Voting",
                newName: "Votings");

            migrationBuilder.RenameTable(
                name: "Vote",
                newName: "Votes");

            migrationBuilder.RenameTable(
                name: "ProfitSharing",
                newName: "ProfitSharings");

            migrationBuilder.RenameTable(
                name: "Profile",
                newName: "Profiles");

            migrationBuilder.RenameTable(
                name: "Payment",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "Novel",
                newName: "Novels");

            migrationBuilder.RenameTable(
                name: "NormalOption",
                newName: "NormalOptions");

            migrationBuilder.RenameTable(
                name: "Message",
                newName: "Messages");

            migrationBuilder.RenameTable(
                name: "Involving",
                newName: "Involvings");

            migrationBuilder.RenameTable(
                name: "Follow",
                newName: "Follows");

            migrationBuilder.RenameTable(
                name: "Feedback",
                newName: "Feedbacks");

            migrationBuilder.RenameTable(
                name: "Episode",
                newName: "Episodes");

            migrationBuilder.RenameTable(
                name: "Dice",
                newName: "Dices");

            migrationBuilder.RenameTable(
                name: "Comment",
                newName: "Comments");

            migrationBuilder.RenameTable(
                name: "BiddingOption",
                newName: "BiddingOptions");

            migrationBuilder.RenameTable(
                name: "Announcement",
                newName: "Announcements");

            migrationBuilder.RenameTable(
                name: "Agree",
                newName: "Agrees");

            migrationBuilder.RenameIndex(
                name: "IX_Voting_EpisodeID",
                table: "Votings",
                newName: "IX_Votings_EpisodeID");

            migrationBuilder.RenameIndex(
                name: "IX_Vote_NormalOptionID",
                table: "Votes",
                newName: "IX_Votes_NormalOptionID");

            migrationBuilder.RenameIndex(
                name: "IX_Vote_BiddingOptionID",
                table: "Votes",
                newName: "IX_Votes_BiddingOptionID");

            migrationBuilder.RenameIndex(
                name: "IX_Profile_SeqNo",
                table: "Profiles",
                newName: "IX_Profiles_SeqNo");

            migrationBuilder.RenameIndex(
                name: "IX_Novel_ProfileID",
                table: "Novels",
                newName: "IX_Novels_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_NormalOption_VotingID",
                table: "NormalOptions",
                newName: "IX_NormalOptions_VotingID");

            migrationBuilder.RenameIndex(
                name: "IX_Message_ProfileID",
                table: "Messages",
                newName: "IX_Messages_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Message_CommentID",
                table: "Messages",
                newName: "IX_Messages_CommentID");

            migrationBuilder.RenameIndex(
                name: "IX_Involving_ProfileID",
                table: "Involvings",
                newName: "IX_Involvings_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Involving_NovelID",
                table: "Involvings",
                newName: "IX_Involvings_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Involving_InvolverID",
                table: "Involvings",
                newName: "IX_Involvings_InvolverID");

            migrationBuilder.RenameIndex(
                name: "IX_Involving_ArticleID",
                table: "Involvings",
                newName: "IX_Involvings_ArticleID");

            migrationBuilder.RenameIndex(
                name: "IX_Follow_ProfileID",
                table: "Follows",
                newName: "IX_Follows_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Follow_NovelID",
                table: "Follows",
                newName: "IX_Follows_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Follow_FollowerID",
                table: "Follows",
                newName: "IX_Follows_FollowerID");

            migrationBuilder.RenameIndex(
                name: "IX_Episode_NovelID",
                table: "Episodes",
                newName: "IX_Episodes_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Dice_CommentID",
                table: "Dices",
                newName: "IX_Dices_CommentID");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_ProfileID",
                table: "Comments",
                newName: "IX_Comments_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_NovelID",
                table: "Comments",
                newName: "IX_Comments_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_FeedbackID",
                table: "Comments",
                newName: "IX_Comments_FeedbackID");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_EpisodeID",
                table: "Comments",
                newName: "IX_Comments_EpisodeID");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_ArticleID",
                table: "Comments",
                newName: "IX_Comments_ArticleID");

            migrationBuilder.RenameIndex(
                name: "IX_Comment_AnnouncementID",
                table: "Comments",
                newName: "IX_Comments_AnnouncementID");

            migrationBuilder.RenameIndex(
                name: "IX_BiddingOption_VotingID",
                table: "BiddingOptions",
                newName: "IX_BiddingOptions_VotingID");

            migrationBuilder.RenameIndex(
                name: "IX_Agree_ProfileID",
                table: "Agrees",
                newName: "IX_Agrees_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Agree_MessageID",
                table: "Agrees",
                newName: "IX_Agrees_MessageID");

            migrationBuilder.RenameIndex(
                name: "IX_Agree_CommentID",
                table: "Agrees",
                newName: "IX_Agrees_CommentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Votings",
                table: "Votings",
                column: "VotingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Votes",
                table: "Votes",
                column: "VoteID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfitSharings",
                table: "ProfitSharings",
                column: "ProfitSharingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles",
                column: "ProfileID")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "PaymentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Novels",
                table: "Novels",
                column: "NovelID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NormalOptions",
                table: "NormalOptions",
                column: "NormalOptionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "MessageID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Involvings",
                table: "Involvings",
                column: "InvolvingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Follows",
                table: "Follows",
                column: "FollowID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Feedbacks",
                table: "Feedbacks",
                column: "FeedbackID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Episodes",
                table: "Episodes",
                column: "EpisodeID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Dices",
                table: "Dices",
                column: "DiceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "CommentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BiddingOptions",
                table: "BiddingOptions",
                column: "BiddingOptionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Announcements",
                table: "Announcements",
                column: "AnnouncementID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Agrees",
                table: "Agrees",
                column: "AgreeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Agrees_Comments_CommentID",
                table: "Agrees",
                column: "CommentID",
                principalTable: "Comments",
                principalColumn: "CommentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Agrees_Messages_MessageID",
                table: "Agrees",
                column: "MessageID",
                principalTable: "Messages",
                principalColumn: "MessageID");

            migrationBuilder.AddForeignKey(
                name: "FK_Agrees_Profiles_ProfileID",
                table: "Agrees",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Profiles_ProfileID",
                table: "Articles",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleViewer_Profiles_ProfileID",
                table: "ArticleViewer",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_BiddingOptions_Votings_VotingID",
                table: "BiddingOptions",
                column: "VotingID",
                principalTable: "Votings",
                principalColumn: "VotingID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Announcements_AnnouncementID",
                table: "Comments",
                column: "AnnouncementID",
                principalTable: "Announcements",
                principalColumn: "AnnouncementID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Articles_ArticleID",
                table: "Comments",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Episodes_EpisodeID",
                table: "Comments",
                column: "EpisodeID",
                principalTable: "Episodes",
                principalColumn: "EpisodeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Feedbacks_FeedbackID",
                table: "Comments",
                column: "FeedbackID",
                principalTable: "Feedbacks",
                principalColumn: "FeedbackID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Novels_NovelID",
                table: "Comments",
                column: "NovelID",
                principalTable: "Novels",
                principalColumn: "NovelID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Profiles_ProfileID",
                table: "Comments",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Dices_Comments_CommentID",
                table: "Dices",
                column: "CommentID",
                principalTable: "Comments",
                principalColumn: "CommentID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Episodes_Novels_NovelID",
                table: "Episodes",
                column: "NovelID",
                principalTable: "Novels",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Follows_Novels_NovelID",
                table: "Follows",
                column: "NovelID",
                principalTable: "Novels",
                principalColumn: "NovelID");

            migrationBuilder.AddForeignKey(
                name: "FK_Follows_Profiles_FollowerID",
                table: "Follows",
                column: "FollowerID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Follows_Profiles_ProfileID",
                table: "Follows",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involvings_Articles_ArticleID",
                table: "Involvings",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involvings_Novels_NovelID",
                table: "Involvings",
                column: "NovelID",
                principalTable: "Novels",
                principalColumn: "NovelID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involvings_Profiles_InvolverID",
                table: "Involvings",
                column: "InvolverID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Involvings_Profiles_ProfileID",
                table: "Involvings",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Comments_CommentID",
                table: "Messages",
                column: "CommentID",
                principalTable: "Comments",
                principalColumn: "CommentID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Profiles_ProfileID",
                table: "Messages",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_Missions_Profiles_ProfileID",
                table: "Missions",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NormalOptions_Votings_VotingID",
                table: "NormalOptions",
                column: "VotingID",
                principalTable: "Votings",
                principalColumn: "VotingID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Profiles_ProfileID",
                table: "Notifications",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelNovelTag_Novels_NovelsNovelID",
                table: "NovelNovelTag",
                column: "NovelsNovelID",
                principalTable: "Novels",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Novels_Profiles_ProfileID",
                table: "Novels",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelViewer_Novels_NovelID",
                table: "NovelViewer",
                column: "NovelID",
                principalTable: "Novels",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelViewer_Profiles_ProfileID",
                table: "NovelViewer",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_NovelViewIp_Novels_NovelsNovelID",
                table: "NovelViewIp",
                column: "NovelsNovelID",
                principalTable: "Novels",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileAchievement_Profiles_ProfileID",
                table: "ProfileAchievement",
                column: "ProfileID",
                principalTable: "Profiles",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_BiddingOptions_BiddingOptionID",
                table: "Votes",
                column: "BiddingOptionID",
                principalTable: "BiddingOptions",
                principalColumn: "BiddingOptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_NormalOptions_NormalOptionID",
                table: "Votes",
                column: "NormalOptionID",
                principalTable: "NormalOptions",
                principalColumn: "NormalOptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Votings_Episodes_EpisodeID",
                table: "Votings",
                column: "EpisodeID",
                principalTable: "Episodes",
                principalColumn: "EpisodeID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agrees_Comments_CommentID",
                table: "Agrees");

            migrationBuilder.DropForeignKey(
                name: "FK_Agrees_Messages_MessageID",
                table: "Agrees");

            migrationBuilder.DropForeignKey(
                name: "FK_Agrees_Profiles_ProfileID",
                table: "Agrees");

            migrationBuilder.DropForeignKey(
                name: "FK_Articles_Profiles_ProfileID",
                table: "Articles");

            migrationBuilder.DropForeignKey(
                name: "FK_ArticleViewer_Profiles_ProfileID",
                table: "ArticleViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_BiddingOptions_Votings_VotingID",
                table: "BiddingOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Announcements_AnnouncementID",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Articles_ArticleID",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Episodes_EpisodeID",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Feedbacks_FeedbackID",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Novels_NovelID",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Profiles_ProfileID",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Dices_Comments_CommentID",
                table: "Dices");

            migrationBuilder.DropForeignKey(
                name: "FK_Episodes_Novels_NovelID",
                table: "Episodes");

            migrationBuilder.DropForeignKey(
                name: "FK_Follows_Novels_NovelID",
                table: "Follows");

            migrationBuilder.DropForeignKey(
                name: "FK_Follows_Profiles_FollowerID",
                table: "Follows");

            migrationBuilder.DropForeignKey(
                name: "FK_Follows_Profiles_ProfileID",
                table: "Follows");

            migrationBuilder.DropForeignKey(
                name: "FK_Involvings_Articles_ArticleID",
                table: "Involvings");

            migrationBuilder.DropForeignKey(
                name: "FK_Involvings_Novels_NovelID",
                table: "Involvings");

            migrationBuilder.DropForeignKey(
                name: "FK_Involvings_Profiles_InvolverID",
                table: "Involvings");

            migrationBuilder.DropForeignKey(
                name: "FK_Involvings_Profiles_ProfileID",
                table: "Involvings");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Comments_CommentID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Profiles_ProfileID",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Missions_Profiles_ProfileID",
                table: "Missions");

            migrationBuilder.DropForeignKey(
                name: "FK_NormalOptions_Votings_VotingID",
                table: "NormalOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Profiles_ProfileID",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelNovelTag_Novels_NovelsNovelID",
                table: "NovelNovelTag");

            migrationBuilder.DropForeignKey(
                name: "FK_Novels_Profiles_ProfileID",
                table: "Novels");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelViewer_Novels_NovelID",
                table: "NovelViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelViewer_Profiles_ProfileID",
                table: "NovelViewer");

            migrationBuilder.DropForeignKey(
                name: "FK_NovelViewIp_Novels_NovelsNovelID",
                table: "NovelViewIp");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileAchievement_Profiles_ProfileID",
                table: "ProfileAchievement");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_BiddingOptions_BiddingOptionID",
                table: "Votes");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_NormalOptions_NormalOptionID",
                table: "Votes");

            migrationBuilder.DropForeignKey(
                name: "FK_Votings_Episodes_EpisodeID",
                table: "Votings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Votings",
                table: "Votings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Votes",
                table: "Votes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfitSharings",
                table: "ProfitSharings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Profiles",
                table: "Profiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Novels",
                table: "Novels");

            migrationBuilder.DropPrimaryKey(
                name: "PK_NormalOptions",
                table: "NormalOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Involvings",
                table: "Involvings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Follows",
                table: "Follows");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Feedbacks",
                table: "Feedbacks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Episodes",
                table: "Episodes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Dices",
                table: "Dices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BiddingOptions",
                table: "BiddingOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Announcements",
                table: "Announcements");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Agrees",
                table: "Agrees");

            migrationBuilder.RenameTable(
                name: "Votings",
                newName: "Voting");

            migrationBuilder.RenameTable(
                name: "Votes",
                newName: "Vote");

            migrationBuilder.RenameTable(
                name: "ProfitSharings",
                newName: "ProfitSharing");

            migrationBuilder.RenameTable(
                name: "Profiles",
                newName: "Profile");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "Payment");

            migrationBuilder.RenameTable(
                name: "Novels",
                newName: "Novel");

            migrationBuilder.RenameTable(
                name: "NormalOptions",
                newName: "NormalOption");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "Message");

            migrationBuilder.RenameTable(
                name: "Involvings",
                newName: "Involving");

            migrationBuilder.RenameTable(
                name: "Follows",
                newName: "Follow");

            migrationBuilder.RenameTable(
                name: "Feedbacks",
                newName: "Feedback");

            migrationBuilder.RenameTable(
                name: "Episodes",
                newName: "Episode");

            migrationBuilder.RenameTable(
                name: "Dices",
                newName: "Dice");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "Comment");

            migrationBuilder.RenameTable(
                name: "BiddingOptions",
                newName: "BiddingOption");

            migrationBuilder.RenameTable(
                name: "Announcements",
                newName: "Announcement");

            migrationBuilder.RenameTable(
                name: "Agrees",
                newName: "Agree");

            migrationBuilder.RenameIndex(
                name: "IX_Votings_EpisodeID",
                table: "Voting",
                newName: "IX_Voting_EpisodeID");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_NormalOptionID",
                table: "Vote",
                newName: "IX_Vote_NormalOptionID");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_BiddingOptionID",
                table: "Vote",
                newName: "IX_Vote_BiddingOptionID");

            migrationBuilder.RenameIndex(
                name: "IX_Profiles_SeqNo",
                table: "Profile",
                newName: "IX_Profile_SeqNo");

            migrationBuilder.RenameIndex(
                name: "IX_Novels_ProfileID",
                table: "Novel",
                newName: "IX_Novel_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_NormalOptions_VotingID",
                table: "NormalOption",
                newName: "IX_NormalOption_VotingID");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_ProfileID",
                table: "Message",
                newName: "IX_Message_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_CommentID",
                table: "Message",
                newName: "IX_Message_CommentID");

            migrationBuilder.RenameIndex(
                name: "IX_Involvings_ProfileID",
                table: "Involving",
                newName: "IX_Involving_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Involvings_NovelID",
                table: "Involving",
                newName: "IX_Involving_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Involvings_InvolverID",
                table: "Involving",
                newName: "IX_Involving_InvolverID");

            migrationBuilder.RenameIndex(
                name: "IX_Involvings_ArticleID",
                table: "Involving",
                newName: "IX_Involving_ArticleID");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_ProfileID",
                table: "Follow",
                newName: "IX_Follow_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_NovelID",
                table: "Follow",
                newName: "IX_Follow_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Follows_FollowerID",
                table: "Follow",
                newName: "IX_Follow_FollowerID");

            migrationBuilder.RenameIndex(
                name: "IX_Episodes_NovelID",
                table: "Episode",
                newName: "IX_Episode_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Dices_CommentID",
                table: "Dice",
                newName: "IX_Dice_CommentID");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_ProfileID",
                table: "Comment",
                newName: "IX_Comment_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_NovelID",
                table: "Comment",
                newName: "IX_Comment_NovelID");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_FeedbackID",
                table: "Comment",
                newName: "IX_Comment_FeedbackID");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_EpisodeID",
                table: "Comment",
                newName: "IX_Comment_EpisodeID");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_ArticleID",
                table: "Comment",
                newName: "IX_Comment_ArticleID");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_AnnouncementID",
                table: "Comment",
                newName: "IX_Comment_AnnouncementID");

            migrationBuilder.RenameIndex(
                name: "IX_BiddingOptions_VotingID",
                table: "BiddingOption",
                newName: "IX_BiddingOption_VotingID");

            migrationBuilder.RenameIndex(
                name: "IX_Agrees_ProfileID",
                table: "Agree",
                newName: "IX_Agree_ProfileID");

            migrationBuilder.RenameIndex(
                name: "IX_Agrees_MessageID",
                table: "Agree",
                newName: "IX_Agree_MessageID");

            migrationBuilder.RenameIndex(
                name: "IX_Agrees_CommentID",
                table: "Agree",
                newName: "IX_Agree_CommentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Voting",
                table: "Voting",
                column: "VotingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vote",
                table: "Vote",
                column: "VoteID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfitSharing",
                table: "ProfitSharing",
                column: "ProfitSharingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Profile",
                table: "Profile",
                column: "ProfileID")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payment",
                table: "Payment",
                column: "PaymentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Novel",
                table: "Novel",
                column: "NovelID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_NormalOption",
                table: "NormalOption",
                column: "NormalOptionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Message",
                table: "Message",
                column: "MessageID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Involving",
                table: "Involving",
                column: "InvolvingID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Follow",
                table: "Follow",
                column: "FollowID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Feedback",
                table: "Feedback",
                column: "FeedbackID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Episode",
                table: "Episode",
                column: "EpisodeID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Dice",
                table: "Dice",
                column: "DiceID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comment",
                table: "Comment",
                column: "CommentID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BiddingOption",
                table: "BiddingOption",
                column: "BiddingOptionID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Announcement",
                table: "Announcement",
                column: "AnnouncementID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Agree",
                table: "Agree",
                column: "AgreeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Agree_Comment_CommentID",
                table: "Agree",
                column: "CommentID",
                principalTable: "Comment",
                principalColumn: "CommentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Agree_Message_MessageID",
                table: "Agree",
                column: "MessageID",
                principalTable: "Message",
                principalColumn: "MessageID");

            migrationBuilder.AddForeignKey(
                name: "FK_Agree_Profile_ProfileID",
                table: "Agree",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Articles_Profile_ProfileID",
                table: "Articles",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticleViewer_Profile_ProfileID",
                table: "ArticleViewer",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_BiddingOption_Voting_VotingID",
                table: "BiddingOption",
                column: "VotingID",
                principalTable: "Voting",
                principalColumn: "VotingID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Announcement_AnnouncementID",
                table: "Comment",
                column: "AnnouncementID",
                principalTable: "Announcement",
                principalColumn: "AnnouncementID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Articles_ArticleID",
                table: "Comment",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Episode_EpisodeID",
                table: "Comment",
                column: "EpisodeID",
                principalTable: "Episode",
                principalColumn: "EpisodeID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Feedback_FeedbackID",
                table: "Comment",
                column: "FeedbackID",
                principalTable: "Feedback",
                principalColumn: "FeedbackID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Novel_NovelID",
                table: "Comment",
                column: "NovelID",
                principalTable: "Novel",
                principalColumn: "NovelID");

            migrationBuilder.AddForeignKey(
                name: "FK_Comment_Profile_ProfileID",
                table: "Comment",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Dice_Comment_CommentID",
                table: "Dice",
                column: "CommentID",
                principalTable: "Comment",
                principalColumn: "CommentID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Episode_Novel_NovelID",
                table: "Episode",
                column: "NovelID",
                principalTable: "Novel",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Novel_NovelID",
                table: "Follow",
                column: "NovelID",
                principalTable: "Novel",
                principalColumn: "NovelID");

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Profile_FollowerID",
                table: "Follow",
                column: "FollowerID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Follow_Profile_ProfileID",
                table: "Follow",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Articles_ArticleID",
                table: "Involving",
                column: "ArticleID",
                principalTable: "Articles",
                principalColumn: "ArticleID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Novel_NovelID",
                table: "Involving",
                column: "NovelID",
                principalTable: "Novel",
                principalColumn: "NovelID");

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Profile_InvolverID",
                table: "Involving",
                column: "InvolverID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Involving_Profile_ProfileID",
                table: "Involving",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Comment_CommentID",
                table: "Message",
                column: "CommentID",
                principalTable: "Comment",
                principalColumn: "CommentID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Profile_ProfileID",
                table: "Message",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_Missions_Profile_ProfileID",
                table: "Missions",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NormalOption_Voting_VotingID",
                table: "NormalOption",
                column: "VotingID",
                principalTable: "Voting",
                principalColumn: "VotingID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Profile_ProfileID",
                table: "Notifications",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Novel_Profile_ProfileID",
                table: "Novel",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelNovelTag_Novel_NovelsNovelID",
                table: "NovelNovelTag",
                column: "NovelsNovelID",
                principalTable: "Novel",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelViewer_Novel_NovelID",
                table: "NovelViewer",
                column: "NovelID",
                principalTable: "Novel",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NovelViewer_Profile_ProfileID",
                table: "NovelViewer",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID");

            migrationBuilder.AddForeignKey(
                name: "FK_NovelViewIp_Novel_NovelsNovelID",
                table: "NovelViewIp",
                column: "NovelsNovelID",
                principalTable: "Novel",
                principalColumn: "NovelID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileAchievement_Profile_ProfileID",
                table: "ProfileAchievement",
                column: "ProfileID",
                principalTable: "Profile",
                principalColumn: "ProfileID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Vote_BiddingOption_BiddingOptionID",
                table: "Vote",
                column: "BiddingOptionID",
                principalTable: "BiddingOption",
                principalColumn: "BiddingOptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Vote_NormalOption_NormalOptionID",
                table: "Vote",
                column: "NormalOptionID",
                principalTable: "NormalOption",
                principalColumn: "NormalOptionID");

            migrationBuilder.AddForeignKey(
                name: "FK_Voting_Episode_EpisodeID",
                table: "Voting",
                column: "EpisodeID",
                principalTable: "Episode",
                principalColumn: "EpisodeID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
