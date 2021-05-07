using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class AddPaymentAndProfitSharing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RtnMsg = table.Column<string>(nullable: true),
                    TradeNo = table.Column<string>(nullable: true),
                    TradeAmt = table.Column<int>(nullable: false),
                    PaymentDate = table.Column<string>(nullable: true),
                    TradeDate = table.Column<string>(nullable: true),
                    ReturnString = table.Column<string>(nullable: true),
                    InvolverID = table.Column<string>(nullable: true),
                    RequestBody = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentID);
                });

            migrationBuilder.CreateTable(
                name: "ProfitSharing",
                columns: table => new
                {
                    ProfitSharingID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvolverID = table.Column<string>(nullable: true),
                    CreditCard = table.Column<string>(nullable: true),
                    SharingValue = table.Column<int>(nullable: false),
                    SharingDone = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfitSharing", x => x.ProfitSharingID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "ProfitSharing");

            migrationBuilder.DropColumn(
                name: "BankAccount",
                table: "AspNetUsers");
        }
    }
}
