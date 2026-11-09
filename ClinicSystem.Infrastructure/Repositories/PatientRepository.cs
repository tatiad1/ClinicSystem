using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class PatientRepository : Repository<Patient>, IPatientRepository
    {
        public PatientRepository(ClinicDbContext context) : base(context)
        {
        }

        public async Task<Patient?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Patients
                .Include(p => p.Person).ThenInclude(person => person.User)
                .Include(p => p.FamilyDoctor!).ThenInclude(d => d.User).ThenInclude(u => u.Person)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        public async Task<ICollection<Patient>> GetAllWithDetailsAsync(int? familyDoctorId = null, CancellationToken cancellationToken = default)
        {
            IQueryable<Patient> query = _context.Patients
                .AsNoTracking()
                .Include(p => p.Person).ThenInclude(person => person.User)
                .Include(p => p.FamilyDoctor!).ThenInclude(d => d.User).ThenInclude(u => u.Person);

            if (familyDoctorId.HasValue)
                query = query.Where(p => p.FamilyDoctorId == familyDoctorId.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<ICollection<Patient>> GetAccessibleToDoctorAsync(
            int doctorId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Patients
                .AsNoTracking()
                .Include(p => p.Person).ThenInclude(person => person.User)
                .Include(p => p.FamilyDoctor!).ThenInclude(d => d.User).ThenInclude(u => u.Person)
                .Where(p => p.FamilyDoctorId == doctorId
                    || p.Appointments.Any(a => a.DoctorId == doctorId
                        && (a.Status == ClinicSystem.Domain.Enums.AppointmentStatus.Approved
                            || a.Status == ClinicSystem.Domain.Enums.AppointmentStatus.Completed)))
                .ToListAsync(cancellationToken);
        }
    }
}
