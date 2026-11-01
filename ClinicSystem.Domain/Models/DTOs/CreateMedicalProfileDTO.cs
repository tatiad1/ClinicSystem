namespace ClinicSystem.Domain.Models.DTOs
{
    public class CreateMedicalProfileDTO
    {
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicConditions { get; set; }
    }
}