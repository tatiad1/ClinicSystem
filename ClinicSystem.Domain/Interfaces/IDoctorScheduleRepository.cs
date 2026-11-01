using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IDoctorScheduleRepository : IRepository<DoctorSchedule>
    {
        Task<ICollection<DoctorSchedule>> GetByDoctorAsync(int doctorId, CancellationToken cancellationToken = default);

        // excludeId გვჭირდება განახლებისას, რომ გრაფიკი საკუთარ თავს არ შეეჯახოს
        Task<bool> HasOverlapAsync(int doctorId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime,
            int? excludeId = null, CancellationToken cancellationToken = default);
    }
}