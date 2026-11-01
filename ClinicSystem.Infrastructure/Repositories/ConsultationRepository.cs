using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class ConsultationRepository : Repository<Consultation>, IConsultationRepository
    {
        public ConsultationRepository(ClinicDbContext context) : base(context)
        {
        }

        private static IQueryable<Consultation> WithDetails(IQueryable<Consultation> query)
        {
            return query
                .Include(c => c.Appointment).ThenInclude(a => a.Patient).ThenInclude(p => p.Person)
                .Include(c => c.Appointment).ThenInclude(a => a.Doctor).ThenInclude(d => d.User).ThenInclude(u => u.Person);
        }

        // თვალყურდევნებულია, რადგან განახლებისთვისაც ვიყენებთ
        public async Task<Consultation?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default)
        {
            return await WithDetails(_context.Consultations)
                .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId, cancellationToken);
        }

        public async Task<ICollection<Consultation>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default)
        {
            return await WithDetails(_context.Consultations.AsNoTracking())
                .Where(c => c.Appointment.PatientId == patientId)
                .OrderByDescending(c => c.Appointment.StartTime)
                .ToListAsync(cancellationToken);
        }
    }
}