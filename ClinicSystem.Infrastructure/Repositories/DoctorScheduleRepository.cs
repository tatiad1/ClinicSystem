using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class DoctorScheduleRepository : Repository<DoctorSchedule>, IDoctorScheduleRepository
    {
        public DoctorScheduleRepository(ClinicDbContext context) : base(context)
        {
        }

        public async Task<ICollection<DoctorSchedule>> GetByDoctorAsync(int doctorId, CancellationToken cancellationToken = default)
        {
            return await _context.DoctorSchedules
                .AsNoTracking()
                .Where(s => s.DoctorId == doctorId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasOverlapAsync(int doctorId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime,
            int? excludeId = null, CancellationToken cancellationToken = default)
        {
            return await _context.DoctorSchedules.AnyAsync(s =>
                s.DoctorId == doctorId
                && s.DayOfWeek == dayOfWeek
                && (excludeId == null || s.Id != excludeId)
                && s.StartTime < endTime
                && startTime < s.EndTime,
                cancellationToken);
        }
    }
}