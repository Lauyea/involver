using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Involver.Data.Migrations
{
    public partial class SaveImageUrlInstead : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImage",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Novel");

            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Profile",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Profile",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Novel",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannerImageUrl",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Profile");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Novel");

            migrationBuilder.AddColumn<byte[]>(
                name: "BannerImage",
                table: "Profile",
                type: "varbinary(max)",
                maxLength: 2097152,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "Profile",
                type: "varbinary(max)",
                maxLength: 2097152,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Image",
                table: "Novel",
                type: "varbinary(max)",
                maxLength: 2097152,
                nullable: true);
        }
    }
}
