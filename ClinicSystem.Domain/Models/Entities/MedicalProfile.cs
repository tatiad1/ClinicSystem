namespace ClinicSystem.Domain.Models.Entities;

public class MedicalProfile : BaseEntity
{
    public string? BloodType { get; set; }
    public string? Allergies { get; set; }
    public string? ChronicConditions { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
}