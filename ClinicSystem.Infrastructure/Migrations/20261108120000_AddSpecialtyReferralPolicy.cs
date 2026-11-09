using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicSystem.Infrastructure.Migrations
{
    public partial class AddSpecialtyReferralPolicy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFamilyMedicine",
                table: "Specialties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresReferral",
                table: "Specialties",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Preserve existing specialty rows while identifying the common English name.
            migrationBuilder.Sql(
                "UPDATE [Specialties] SET [IsFamilyMedicine] = 1 " +
                "WHERE LOWER(LTRIM(RTRIM([Name]))) = N'family medicine';");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Specialties_FamilyMedicine_NoReferral",
                table: "Specialties",
                sql: "NOT ([IsFamilyMedicine] = 1 AND [RequiresReferral] = 1)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Specialties_FamilyMedicine_NoReferral",
                table: "Specialties");

            migrationBuilder.DropColumn(
                name: "IsFamilyMedicine",
                table: "Specialties");

            migrationBuilder.DropColumn(
                name: "RequiresReferral",
                table: "Specialties");
        }
    }
}
