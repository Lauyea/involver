using Microsoft.EntityFrameworkCore.Migrations;

namespace Involver.Data.Migrations
{
    public partial class AddPaymentAdditionalParameters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MerchantTradeNo",
                table: "Payment",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RtnCode",
                table: "Payment",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SimulatePaid",
                table: "Payment",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MerchantTradeNo",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "RtnCode",
                table: "Payment");

            migrationBuilder.DropColumn(
                name: "SimulatePaid",
                table: "Payment");
        }
    }
}
