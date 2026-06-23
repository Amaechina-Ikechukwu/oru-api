using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ORUApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFeesToStudyLevelAndApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApplicationFee",
                table: "StudyLevels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TuitionFee",
                table: "StudyLevels",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationFeeReceiptUrl",
                table: "Applications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationFee",
                table: "StudyLevels");

            migrationBuilder.DropColumn(
                name: "TuitionFee",
                table: "StudyLevels");

            migrationBuilder.DropColumn(
                name: "ApplicationFeeReceiptUrl",
                table: "Applications");
        }
    }
}
