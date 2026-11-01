namespace ClinicSystem.Domain.Models.DTOs
{
    public class PatientDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateOnly BirthDate { get; set; }
        public int? FamilyDoctorId { get; set; }
        public string? FamilyDoctorName { get; set; }
        public bool IsRegistered { get; set; }
    }
}