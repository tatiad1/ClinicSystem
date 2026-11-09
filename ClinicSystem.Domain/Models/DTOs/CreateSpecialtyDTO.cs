namespace ClinicSystem.Domain.Models.DTOs
{
    public class CreateSpecialtyDTO
    {
        public string Name { get; set; } = string.Empty;
        public bool IsFamilyMedicine { get; set; }
        public bool RequiresReferral { get; set; }
    }
}
