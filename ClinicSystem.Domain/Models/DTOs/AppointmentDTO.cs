using ClinicSystem.Domain.Enums;

namespace ClinicSystem.Domain.Models.DTOs
{
    public class AppointmentDTO
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime => StartTime.AddMinutes(AppointmentRules.SlotMinutes);
        public AppointmentStatus Status { get; set; }
        public int? ReferralId { get; set; }
    }
}