using ClinicSystem.Domain.Models.Identity;

namespace ClinicSystem.Domain.Models.Entities;

public class Doctor : BaseEntity
{
    public bool IsActive { get; set; } = true;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Patient> FamilyPatients { get; set; } = new List<Patient>();
}