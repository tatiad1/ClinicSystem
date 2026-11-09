namespace ClinicSystem.Domain.Models.DTOs
{
    public class SpecialtyDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsFamilyMedicine { get; set; }
        public bool RequiresReferral { get; set; }
    }
}
