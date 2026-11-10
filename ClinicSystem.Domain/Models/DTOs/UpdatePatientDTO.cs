namespace ClinicSystem.Domain.Models.DTOs
{
    public class UpdatePatientDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? ContactEmail { get; set; }
        public DateOnly BirthDate { get; set; }
    }
}
