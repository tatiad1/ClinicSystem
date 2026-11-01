namespace ClinicSystem.Domain.Models.DTOs
{
    public class ConsultationDTO
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public DateTime AppointmentTime { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string? Prescription { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}