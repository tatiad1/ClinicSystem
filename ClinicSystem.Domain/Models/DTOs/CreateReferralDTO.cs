namespace ClinicSystem.Domain.Models.DTOs
{
    public class CreateReferralDTO
    {
        public int FromDoctorId { get; set; }
        public int ToSpecialtyId { get; set; }
        public string? Reason { get; set; }

        // რამდენი დღე მოქმედებს; თუ არ მიუთითე, 30
        public int? ValidForDays { get; set; }
    }
}