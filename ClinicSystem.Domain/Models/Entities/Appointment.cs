using ClinicSystem.Domain.Enums;

namespace ClinicSystem.Domain.Models.Entities;

public class Appointment : BaseEntity
{
    public DateTime StartTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public bool ReminderSent { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public int? ReferralId { get; set; }
    public Referral? Referral { get; set; }

    public Consultation? Consultation { get; set; }
}