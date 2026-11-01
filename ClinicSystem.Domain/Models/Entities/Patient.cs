namespace ClinicSystem.Domain.Models.Entities;

public class Patient : BaseEntity
{
    public DateOnly BirthDate { get; set; }

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public int? FamilyDoctorId { get; set; }
    public Doctor? FamilyDoctor { get; set; }

    // ბაზაში არ ინახება: რეგისტრირებულია, თუ ოჯახის ექიმი ჰყავს
    public bool IsRegistered => FamilyDoctorId != null;

    public MedicalProfile? MedicalProfile { get; set; }
    public ICollection<Referral> Referrals { get; set; } = new List<Referral>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}