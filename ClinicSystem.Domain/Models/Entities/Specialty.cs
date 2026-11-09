namespace ClinicSystem.Domain.Models.Entities;

public class Specialty : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsFamilyMedicine { get; set; }
    public bool RequiresReferral { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
