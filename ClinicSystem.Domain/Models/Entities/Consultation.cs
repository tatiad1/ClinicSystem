namespace ClinicSystem.Domain.Models.Entities;

public class Consultation : BaseEntity
{
    public string Diagnosis { get; set; } = string.Empty;
    public string? Prescription { get; set; }

    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
}