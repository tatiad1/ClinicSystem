using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(ClinicDbContext context) : base(context)
        {
        }

        private static IQueryable<Appointment> WithDetails(IQueryable<Appointment> query)
        {
            return query
                .Include(a => a.Patient).ThenInclude(p => p.Person).ThenInclude(person => person.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User).ThenInclude(u => u.Person)
                .Include(a => a.Doctor).ThenInclude(d => d.Specialty);
        }

        public async Task<Appointment?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await WithDetails(_context.Appointments)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<ICollection<Appointment>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default)
        {
            return await WithDetails(_context.Appointments.AsNoTracking())
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<ICollection<Appointment>> GetByDoctorAsync(int doctorId, DateOnly? date, CancellationToken cancellationToken = default)
        {
            var query = WithDetails(_context.Appointments.AsNoTracking())
                .Where(a => a.DoctorId == doctorId);

            if (date.HasValue)
            {
                var from = date.Value.ToDateTime(TimeOnly.MinValue);
                var to = from.AddDays(1);
                query = query.Where(a => a.StartTime >= from && a.StartTime < to);
            }

            return await query
                .OrderBy(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<ICollection<DateTime>> GetActiveStartTimesAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default)
        {
            var from = date.ToDateTime(TimeOnly.MinValue);
            var to = from.AddDays(1);

            return await _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId
                    && a.StartTime >= from
                    && a.StartTime < to
                    && a.Status == AppointmentStatus.Booked)
                .Select(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<ICollection<DateTime>> GetFutureActiveStartTimesAsync(int doctorId, DateTime from, CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId
                    && a.StartTime > from
                    && a.Status == AppointmentStatus.Booked)
                .Select(a => a.StartTime)
                .ToListAsync(cancellationToken);
        }

        // თვალყურდევნებულია, რადგან შემდეგ ReminderSent იცვლება
        public async Task<ICollection<Appointment>> GetDueForReminderAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            return await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.Person).ThenInclude(person => person.User)
                .Include(a => a.Doctor).ThenInclude(d => d.User).ThenInclude(u => u.Person)
                .Include(a => a.Doctor).ThenInclude(d => d.Specialty)
                .Where(a => a.Status == AppointmentStatus.Booked
                    && !a.ReminderSent
                    && a.StartTime > from
                    && a.StartTime <= to
                    && (a.Patient.Person.ContactEmail != null
                        || (a.Patient.Person.User != null && a.Patient.Person.User.Email != null)))
                .ToListAsync(cancellationToken);
        }
    }
}
