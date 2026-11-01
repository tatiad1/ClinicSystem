using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class DoctorRepository : Repository<Doctor>, IDoctorRepository
    {
        public DoctorRepository(ClinicDbContext context) : base(context)
        {
        }

        public async Task<Doctor?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Doctors
                .Include(d => d.User).ThenInclude(u => u.Person)
                .Include(d => d.Specialty)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<ICollection<Doctor>> GetAllWithDetailsAsync(int? specialtyId = null, CancellationToken cancellationToken = default)
        {
            IQueryable<Doctor> query = _context.Doctors
                .AsNoTracking()
                .Include(d => d.User).ThenInclude(u => u.Person)
                .Include(d => d.Specialty);

            if (specialtyId.HasValue)
                query = query.Where(d => d.SpecialtyId == specialtyId.Value);

            return await query.ToListAsync(cancellationToken);
        }
    }
}