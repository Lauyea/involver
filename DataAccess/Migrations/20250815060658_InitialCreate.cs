using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Achievements",
                columns: table => new
                {
                    AchievementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Rank = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Achievements", x => x.AchievementID);
                });

            migrationBuilder.CreateTable(
                name: "Announcement",
                columns: table => new
                {
                    AnnouncementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcement", x => x.AnnouncementID);
                });

            migrationBuilder.CreateTable(
                name: "ArticleTags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleTags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Prime = table.Column<bool>(type: "bit", nullable: false),
                    BankAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Banned = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    FeedbackID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OwnerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Block = table.Column<bool>(type: "bit", nullable: false),
                    Accept = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.FeedbackID);
                });

            migrationBuilder.CreateTable(
                name: "NovelTags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelTags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RtnMsg = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TradeNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TradeAmt = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TradeDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReturnString = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvolverID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MerchantTradeNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RtnCode = table.Column<int>(type: "int", nullable: false),
                    SimulatePaid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentID);
                });

            migrationBuilder.CreateTable(
                name: "Profile",
                columns: table => new
                {
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Introduction = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RealCoins = table.Column<decimal>(type: "money", nullable: false),
                    VirtualCoins = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyCoins = table.Column<decimal>(type: "money", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastTimeLogin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Professional = table.Column<bool>(type: "bit", nullable: false),
                    Prime = table.Column<bool>(type: "bit", nullable: false),
                    Banned = table.Column<bool>(type: "bit", nullable: false),
                    CanChangeUserName = table.Column<bool>(type: "bit", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    UsedCoins = table.Column<decimal>(type: "money", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile", x => x.ProfileID)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharing",
                columns: table => new
                {
                    ProfitSharingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvolverID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditCard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SharingValue = table.Column<int>(type: "int", nullable: false),
                    SharingDone = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharing", x => x.ProfitSharingID);
                });

            migrationBuilder.CreateTable(
                name: "ViewIps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ip = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViewIps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Article",
                columns: table => new
                {
                    ArticleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Block = table.Column<bool>(type: "bit", nullable: false),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyCoins = table.Column<decimal>(type: "money", nullable: false),
                    ViewRecordJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailyView = table.Column<int>(type: "int", nullable: false),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Article", x => x.ArticleID);
                    table.ForeignKey(
                        name: "FK_Article_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    MissionsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WatchArticle = table.Column<bool>(type: "bit", nullable: false),
                    Vote = table.Column<bool>(type: "bit", nullable: false),
                    LeaveComment = table.Column<bool>(type: "bit", nullable: false),
                    ViewAnnouncement = table.Column<bool>(type: "bit", nullable: false),
                    ShareCreation = table.Column<bool>(type: "bit", nullable: false),
                    BeAgreed = table.Column<bool>(type: "bit", nullable: false),
                    CompleteOtherMissions = table.Column<bool>(type: "bit", nullable: false),
                    DailyLogin = table.Column<bool>(type: "bit", nullable: false),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.MissionsID);
                    table.ForeignKey(
                        name: "FK_Missions_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Novel",
                columns: table => new
                {
                    NovelID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Introduction = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyCoins = table.Column<decimal>(type: "money", nullable: false),
                    PrimeRead = table.Column<bool>(type: "bit", nullable: false),
                    End = table.Column<bool>(type: "bit", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    Block = table.Column<bool>(type: "bit", nullable: false),
                    ViewRecordJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DailyView = table.Column<int>(type: "int", nullable: false),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Novel", x => x.NovelID);
                    table.ForeignKey(
                        name: "FK_Novel_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileAchievement",
                columns: table => new
                {
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AchievementID = table.Column<int>(type: "int", nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    AchieveDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileAchievement", x => new { x.ProfileID, x.AchievementID })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_ProfileAchievement_Achievements_AchievementID",
                        column: x => x.AchievementID,
                        principalTable: "Achievements",
                        principalColumn: "AchievementID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileAchievement_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleArticleTag",
                columns: table => new
                {
                    ArticleTagsTagId = table.Column<int>(type: "int", nullable: false),
                    ArticlesArticleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleArticleTag", x => new { x.ArticleTagsTagId, x.ArticlesArticleID });
                    table.ForeignKey(
                        name: "FK_ArticleArticleTag_ArticleTags_ArticleTagsTagId",
                        column: x => x.ArticleTagsTagId,
                        principalTable: "ArticleTags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleArticleTag_Article_ArticlesArticleID",
                        column: x => x.ArticlesArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleViewer",
                columns: table => new
                {
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArticleID = table.Column<int>(type: "int", nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    ViewDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleViewer", x => new { x.ProfileID, x.ArticleID })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_ArticleViewer_Article_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleViewer_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID");
                });

            migrationBuilder.CreateTable(
                name: "ArticleViewIp",
                columns: table => new
                {
                    ArticlesArticleID = table.Column<int>(type: "int", nullable: false),
                    ViewIpsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleViewIp", x => new { x.ArticlesArticleID, x.ViewIpsId });
                    table.ForeignKey(
                        name: "FK_ArticleViewIp_Article_ArticlesArticleID",
                        column: x => x.ArticlesArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleViewIp_ViewIps_ViewIpsId",
                        column: x => x.ViewIpsId,
                        principalTable: "ViewIps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Episode",
                columns: table => new
                {
                    EpisodeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 65536, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Views = table.Column<int>(type: "int", nullable: false),
                    HasVoting = table.Column<bool>(type: "bit", nullable: false),
                    IsLast = table.Column<bool>(type: "bit", nullable: false),
                    NovelID = table.Column<int>(type: "int", nullable: false)
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
                    FollowID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FollowerID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProfileMonthlyInvolver = table.Column<bool>(type: "bit", nullable: false),
                    NovelMonthlyInvolver = table.Column<bool>(type: "bit", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    NovelID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Follow", x => x.FollowID);
                    table.ForeignKey(
                        name: "FK_Follow_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID");
                    table.ForeignKey(
                        name: "FK_Follow_Profile_FollowerID",
                        column: x => x.FollowerID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Follow_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID");
                });

            migrationBuilder.CreateTable(
                name: "Involving",
                columns: table => new
                {
                    InvolvingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvolverID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<decimal>(type: "money", nullable: false),
                    MonthlyValue = table.Column<decimal>(type: "money", nullable: false),
                    TotalValue = table.Column<decimal>(type: "money", nullable: false),
                    LastTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    NovelID = table.Column<int>(type: "int", nullable: true),
                    ArticleID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Involving", x => x.InvolvingID);
                    table.ForeignKey(
                        name: "FK_Involving_Article_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID");
                    table.ForeignKey(
                        name: "FK_Involving_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID");
                    table.ForeignKey(
                        name: "FK_Involving_Profile_InvolverID",
                        column: x => x.InvolverID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Involving_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID");
                });

            migrationBuilder.CreateTable(
                name: "NovelNovelTag",
                columns: table => new
                {
                    NovelTagsTagId = table.Column<int>(type: "int", nullable: false),
                    NovelsNovelID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelNovelTag", x => new { x.NovelTagsTagId, x.NovelsNovelID });
                    table.ForeignKey(
                        name: "FK_NovelNovelTag_NovelTags_NovelTagsTagId",
                        column: x => x.NovelTagsTagId,
                        principalTable: "NovelTags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NovelNovelTag_Novel_NovelsNovelID",
                        column: x => x.NovelsNovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NovelViewer",
                columns: table => new
                {
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NovelID = table.Column<int>(type: "int", nullable: false),
                    SeqNo = table.Column<int>(type: "int", nullable: false),
                    ViewDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelViewer", x => new { x.ProfileID, x.NovelID })
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_NovelViewer_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NovelViewer_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID");
                });

            migrationBuilder.CreateTable(
                name: "NovelViewIp",
                columns: table => new
                {
                    NovelsNovelID = table.Column<int>(type: "int", nullable: false),
                    ViewIpsId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovelViewIp", x => new { x.NovelsNovelID, x.ViewIpsId });
                    table.ForeignKey(
                        name: "FK_NovelViewIp_Novel_NovelsNovelID",
                        column: x => x.NovelsNovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NovelViewIp_ViewIps_ViewIpsId",
                        column: x => x.ViewIpsId,
                        principalTable: "ViewIps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comment",
                columns: table => new
                {
                    CommentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 16384, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Block = table.Column<bool>(type: "bit", nullable: false),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NovelID = table.Column<int>(type: "int", nullable: true),
                    EpisodeID = table.Column<int>(type: "int", nullable: true),
                    AnnouncementID = table.Column<int>(type: "int", nullable: true),
                    FeedbackID = table.Column<int>(type: "int", nullable: true),
                    ArticleID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comment", x => x.CommentID);
                    table.ForeignKey(
                        name: "FK_Comment_Announcement_AnnouncementID",
                        column: x => x.AnnouncementID,
                        principalTable: "Announcement",
                        principalColumn: "AnnouncementID");
                    table.ForeignKey(
                        name: "FK_Comment_Article_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Article",
                        principalColumn: "ArticleID");
                    table.ForeignKey(
                        name: "FK_Comment_Episode_EpisodeID",
                        column: x => x.EpisodeID,
                        principalTable: "Episode",
                        principalColumn: "EpisodeID");
                    table.ForeignKey(
                        name: "FK_Comment_Feedback_FeedbackID",
                        column: x => x.FeedbackID,
                        principalTable: "Feedback",
                        principalColumn: "FeedbackID");
                    table.ForeignKey(
                        name: "FK_Comment_Novel_NovelID",
                        column: x => x.NovelID,
                        principalTable: "Novel",
                        principalColumn: "NovelID");
                    table.ForeignKey(
                        name: "FK_Comment_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Voting",
                columns: table => new
                {
                    VotingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Policy = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    Limit = table.Column<int>(type: "int", nullable: false),
                    Threshold = table.Column<int>(type: "int", nullable: false),
                    BiddingLowerLimit = table.Column<int>(type: "int", nullable: true),
                    NumberLimit = table.Column<int>(type: "int", nullable: true),
                    CoinLimit = table.Column<int>(type: "int", nullable: true),
                    End = table.Column<bool>(type: "bit", nullable: false),
                    TotalNumber = table.Column<int>(type: "int", nullable: false),
                    TotalCoins = table.Column<int>(type: "int", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeadLine = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EpisodeID = table.Column<int>(type: "int", nullable: false)
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
                    DiceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sides = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
                    CommentID = table.Column<int>(type: "int", nullable: false)
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
                    MessageID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Block = table.Column<bool>(type: "bit", nullable: false),
                    CommentID = table.Column<int>(type: "int", nullable: false),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: true)
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
                        principalColumn: "ProfileID");
                });

            migrationBuilder.CreateTable(
                name: "BiddingOption",
                columns: table => new
                {
                    BiddingOptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BiddingCoins = table.Column<decimal>(type: "money", nullable: false),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    VotingID = table.Column<int>(type: "int", nullable: false)
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
                    NormalOptionID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalCoins = table.Column<decimal>(type: "money", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    VotingID = table.Column<int>(type: "int", nullable: false)
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
                    AgreeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CommentID = table.Column<int>(type: "int", nullable: true),
                    MessageID = table.Column<int>(type: "int", nullable: true),
                    ProfileID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agree", x => x.AgreeID);
                    table.ForeignKey(
                        name: "FK_Agree_Comment_CommentID",
                        column: x => x.CommentID,
                        principalTable: "Comment",
                        principalColumn: "CommentID");
                    table.ForeignKey(
                        name: "FK_Agree_Message_MessageID",
                        column: x => x.MessageID,
                        principalTable: "Message",
                        principalColumn: "MessageID");
                    table.ForeignKey(
                        name: "FK_Agree_Profile_ProfileID",
                        column: x => x.ProfileID,
                        principalTable: "Profile",
                        principalColumn: "ProfileID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vote",
                columns: table => new
                {
                    VoteID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<decimal>(type: "money", nullable: false),
                    BiddingOptionID = table.Column<int>(type: "int", nullable: true),
                    NormalOptionID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vote", x => x.VoteID);
                    table.ForeignKey(
                        name: "FK_Vote_BiddingOption_BiddingOptionID",
                        column: x => x.BiddingOptionID,
                        principalTable: "BiddingOption",
                        principalColumn: "BiddingOptionID");
                    table.ForeignKey(
                        name: "FK_Vote_NormalOption_NormalOptionID",
                        column: x => x.NormalOptionID,
                        principalTable: "NormalOption",
                        principalColumn: "NormalOptionID");
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
                name: "IX_ArticleArticleTag_ArticlesArticleID",
                table: "ArticleArticleTag",
                column: "ArticlesArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewer_ArticleID",
                table: "ArticleViewer",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewer_SeqNo",
                table: "ArticleViewer",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleViewIp_ViewIpsId",
                table: "ArticleViewIp",
                column: "ViewIpsId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

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
                name: "IX_Involving_InvolverID",
                table: "Involving",
                column: "InvolverID");

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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NormalOption_VotingID",
                table: "NormalOption",
                column: "VotingID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProfileID",
                table: "Notifications",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_Novel_ProfileID",
                table: "Novel",
                column: "ProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_NovelNovelTag_NovelsNovelID",
                table: "NovelNovelTag",
                column: "NovelsNovelID");

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewer_NovelID",
                table: "NovelViewer",
                column: "NovelID");

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewer_SeqNo",
                table: "NovelViewer",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_NovelViewIp_ViewIpsId",
                table: "NovelViewIp",
                column: "ViewIpsId");

            migrationBuilder.CreateIndex(
                name: "IX_Profile_SeqNo",
                table: "Profile",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAchievement_AchievementID",
                table: "ProfileAchievement",
                column: "AchievementID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileAchievement_SeqNo",
                table: "ProfileAchievement",
                column: "SeqNo",
                unique: true)
                .Annotation("SqlServer:Clustered", true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agree");

            migrationBuilder.DropTable(
                name: "ArticleArticleTag");

            migrationBuilder.DropTable(
                name: "ArticleViewer");

            migrationBuilder.DropTable(
                name: "ArticleViewIp");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Dice");

            migrationBuilder.DropTable(
                name: "Follow");

            migrationBuilder.DropTable(
                name: "Involving");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NovelNovelTag");

            migrationBuilder.DropTable(
                name: "NovelViewer");

            migrationBuilder.DropTable(
                name: "NovelViewIp");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "ProfileAchievement");

            migrationBuilder.DropTable(
                name: "ProfitSharing");

            migrationBuilder.DropTable(
                name: "Vote");

            migrationBuilder.DropTable(
                name: "Message");

            migrationBuilder.DropTable(
                name: "ArticleTags");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "NovelTags");

            migrationBuilder.DropTable(
                name: "ViewIps");

            migrationBuilder.DropTable(
                name: "Achievements");

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