namespace ClinicSystem.Domain.Models.DTOs
{
    public class UpdateDoctorDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int SpecialtyId { get; set; }
    }
}