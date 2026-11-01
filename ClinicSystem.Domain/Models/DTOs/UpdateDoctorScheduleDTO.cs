namespace ClinicSystem.Domain.Models.DTOs
{
    public class UpdateDoctorScheduleDTO
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }
}