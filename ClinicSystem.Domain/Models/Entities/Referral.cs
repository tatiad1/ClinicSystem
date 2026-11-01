namespace ClinicSystem.Domain.Models.Entities;

public class Referral : BaseEntity
{
    public string? Reason { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int FromDoctorId { get; set; }
    public Doctor FromDoctor { get; set; } = null!;

    public int ToSpecialtyId { get; set; }
    public Specialty ToSpecialty { get; set; } = null!;
}