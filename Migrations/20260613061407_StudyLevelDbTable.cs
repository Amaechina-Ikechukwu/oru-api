using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ORUApi.Migrations
{
    /// <inheritdoc />
    public partial class StudyLevelDbTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Duration = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyLevels", x => x.Id);
                });

            // Fix any invalid StudyLevel values before adding FK constraint
            // 0 = None (should not exist), other out-of-range values default to Undergraduate (3)
            migrationBuilder.Sql("""
                UPDATE "Applications" SET "StudyLevel" = 3 WHERE "StudyLevel" NOT IN (1, 2, 3, 4, 5);
            """);

            // Seed study levels so the FK constraint won't fail on existing applications
            migrationBuilder.Sql("""
                INSERT INTO "StudyLevels" ("Id", "Name", "Description", "Duration", "SortOrder", "IsActive", "CreatedAt")
                VALUES
                    (1, 'Certificate', 'Professional Certifications & Short-Term Courses (3-6 months or 1 year)', '3-12 months', 1, true, now()),
                    (2, 'Diploma', 'Diploma Programs (2 years)', '2 years', 2, true, now()),
                    (3, 'Undergraduate', 'Undergraduate / Bachelor''s Degree (4 years)', '4 years', 3, true, now()),
                    (4, 'Postgraduate Diploma', 'Postgraduate Diploma (PGDE, PDE)', '1-2 years', 4, true, now()),
                    (5, 'Masters', 'Master''s Degree (M.Ed., etc.)', '1-2 years', 5, true, now());
            """);

            // Reset identity sequence to continue after seed IDs
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"StudyLevels\"', 'Id'), 5);");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_StudyLevel",
                table: "Applications",
                column: "StudyLevel");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_StudyLevels_StudyLevel",
                table: "Applications",
                column: "StudyLevel",
                principalTable: "StudyLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_StudyLevels_StudyLevel",
                table: "Applications");

            migrationBuilder.DropTable(
                name: "StudyLevels");

            migrationBuilder.DropIndex(
                name: "IX_Applications_StudyLevel",
                table: "Applications");
        }
    }
}
