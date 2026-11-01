namespace ClinicSystem.Domain.Models.DTOs
{
    public class ReferralDTO
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public int FromDoctorId { get; set; }
        public string FromDoctorName { get; set; } = string.Empty;
        public int ToSpecialtyId { get; set; }
        public string ToSpecialtyName { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }

        // გამოუყენებელია და ვადა არ გასულა
        public bool IsActive { get; set; }
    }
}