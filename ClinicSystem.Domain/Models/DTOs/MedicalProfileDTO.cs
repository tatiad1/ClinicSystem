namespace ClinicSystem.Domain.Models.DTOs
{
    public class MedicalProfileDTO
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicConditions { get; set; }
    }
}