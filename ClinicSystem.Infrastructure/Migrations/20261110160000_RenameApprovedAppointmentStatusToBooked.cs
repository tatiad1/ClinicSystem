using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicSystem.Infrastructure.Migrations
{
    [DbContext(typeof(ClinicDbContext))]
    [Migration("20261110160000_RenameApprovedAppointmentStatusToBooked")]
    public partial class RenameApprovedAppointmentStatusToBooked : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Appointments] SET [Status] = 'Booked' WHERE [Status] IN ('Approved', 'Pending');");

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
                filter: "[Status] = 'Booked'");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_StartTime",
                table: "Appointments",
                columns: new[] { "PatientId", "StartTime" },
                unique: true,
                filter: "[Status] = 'Booked'");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ReferralId",
                table: "Appointments",
                column: "ReferralId",
                unique: true,
                filter: "[ReferralId] IS NOT NULL AND [Status] = 'Booked'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Appointments] SET [Status] = 'Approved' WHERE [Status] = 'Booked';");

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
    }
}
