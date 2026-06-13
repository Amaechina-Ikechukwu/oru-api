using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using ORUApi.Models;

#nullable disable

namespace ORUApi.Migrations
{
    /// <inheritdoc />
    public partial class AddInstallmentSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<InstallmentSubmission>>(
                name: "InstallmentSubmissions",
                table: "Students",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InstallmentSubmissions",
                table: "Students");
        }
    }
}
