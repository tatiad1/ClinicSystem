namespace ClinicSystem.Domain.Models.DTOs
{
    public class CreatePatientDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PersonalNumber { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public DateOnly BirthDate { get; set; }
        public int? FamilyDoctorId { get; set; }

        // Contact address for reminders; does not create a login account by itself.
        public string? ContactEmail { get; set; }

        // ანგარიში არასავალდებულოა
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
