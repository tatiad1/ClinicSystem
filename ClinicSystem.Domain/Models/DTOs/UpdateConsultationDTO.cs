namespace ClinicSystem.Domain.Models.DTOs
{
    public class UpdateConsultationDTO
    {
        public string Diagnosis { get; set; } = string.Empty;
        public string? Prescription { get; set; }
    }
}