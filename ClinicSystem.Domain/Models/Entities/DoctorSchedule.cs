namespace ClinicSystem.Domain.Models.Entities;

public class DoctorSchedule : BaseEntity
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;
}