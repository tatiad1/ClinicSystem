using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ClinicDbContext))]
    [Migration("20261110150000_RemovePendingRejectedAppointmentStatuses")]
    public partial class RemovePendingRejectedAppointmentStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve any historical rows while mapping removed statuses to current equivalents.
            migrationBuilder.Sql("UPDATE [Appointments] SET [Status] = 'Approved' WHERE [Status] = 'Pending';");
            migrationBuilder.Sql("UPDATE [Appointments] SET [Status] = 'Cancelled' WHERE [Status] = 'Rejected';");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "StartTime" },
                unique: true,
                filter: "[Status] = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_StartTime",
                table: "Appointments",
                columns: new[] { "PatientId", "StartTime" },
                unique: true,
                filter: "[Status] = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments",
                column: "ReferralId",
                unique: true,
                filter: "[ReferralId] IS NOT NULL AND [Status] = 'Approved'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "StartTime" },
                unique: true,
                filter: "[Status] IN ('Pending', 'Approved')");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_StartTime",
                table: "Appointments",
                columns: new[] { "PatientId", "StartTime" },
                unique: true,
                filter: "[Status] IN ('Pending', 'Approved')");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments",
                column: "ReferralId",
                unique: true,
                filter: "[ReferralId] IS NOT NULL AND [Status] IN ('Pending', 'Approved')");
        }
    }
}
