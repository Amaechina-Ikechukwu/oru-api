using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ORUApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Admins",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiresAt",
                table: "Admins",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationToken",
                table: "Admins",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "TokenExpiresAt",
                table: "Admins");

            migrationBuilder.DropColumn(
                name: "VerificationToken",
                table: "Admins");
        }
    }
}
