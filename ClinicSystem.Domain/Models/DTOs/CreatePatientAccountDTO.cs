namespace ClinicSystem.Domain.Models.DTOs
{
    public class CreatePatientAccountDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}