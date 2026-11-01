namespace ClinicSystem.Domain.Models.DTOs
{
    public class CreateAppointmentDTO
    {
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public DateTime StartTime { get; set; }

        // საჭიროა მხოლოდ მაშინ, როცა პაციენტი ოჯახის ექიმის გარდა სხვა ექიმთან ეწერება
        public int? ReferralId { get; set; }
    }
}