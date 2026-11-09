using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class SpecialtyRepository : Repository<Specialty>, ISpecialtyRepository
    {
        public SpecialtyRepository(ClinicDbContext context) : base(context)
        {
        }

        public async Task<bool> HasDoctorsAsync(int specialtyId, CancellationToken cancellationToken = default)
        {
            return await _context.Doctors.AnyAsync(d => d.SpecialtyId == specialtyId, cancellationToken);
        }

        public async Task<bool> HasReferralsAsync(int specialtyId, CancellationToken cancellationToken = default)
        {
            return await _context.Referrals.AnyAsync(r => r.ToSpecialtyId == specialtyId, cancellationToken);
        }
    }
}