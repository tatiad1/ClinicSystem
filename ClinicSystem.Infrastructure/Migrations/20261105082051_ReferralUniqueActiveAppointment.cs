using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReferralUniqueActiveAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments",
                column: "ReferralId",
                unique: true,
                filter: "[ReferralId] IS NOT NULL AND [Status] IN ('Pending', 'Approved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments",
                column: "ReferralId");
        }
    }
}
