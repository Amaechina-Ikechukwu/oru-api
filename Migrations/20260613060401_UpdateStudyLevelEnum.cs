using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ORUApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudyLevelEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old enum: Undergraduate=0, Postgraduate=1, Diploma=2, Certificate=3
            // New enum: None=0, Certificate=1, Diploma=2, Undergraduate=3, PostgraduateDiploma=4, Masters=5
            // Remap using CASE to avoid interference between sequential UPDATEs
            migrationBuilder.Sql("""
                UPDATE "Applications" SET "StudyLevel" = CASE "StudyLevel"
                    WHEN 0 THEN 3
                    WHEN 1 THEN 4
                    WHEN 3 THEN 1
                    ELSE "StudyLevel"
                END;
            """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Applications" SET "StudyLevel" = 0 WHERE "StudyLevel" = 3;
                UPDATE "Applications" SET "StudyLevel" = 1 WHERE "StudyLevel" = 4;
                UPDATE "Applications" SET "StudyLevel" = 3 WHERE "StudyLevel" = 1;
            """);
        }
    }
}
