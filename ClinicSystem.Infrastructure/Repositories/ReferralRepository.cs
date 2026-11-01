using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class ReferralRepository : Repository<Referral>, IReferralRepository
    {
        public ReferralRepository(ClinicDbContext context) : base(context)
        {
        }

        private static IQueryable<Referral> WithDetails(IQueryable<Referral> query)
        {
            return query
                .Include(r => r.Patient).ThenInclude(p => p.Person)
                .Include(r => r.FromDoctor).ThenInclude(d => d.User).ThenInclude(u => u.Person)
                .Include(r => r.ToSpecialty);
        }

        public async Task<Referral?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await WithDetails(_context.Referrals)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public async Task<ICollection<Referral>> GetByPatientAsync(int patientId, bool activeOnly, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var query = WithDetails(_context.Referrals.AsNoTracking())
                .Where(r => r.PatientId == patientId);

            if (activeOnly)
                query = query.Where(r => !r.IsUsed && r.ExpiresAt > now);

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasActiveReferralAsync(int patientId, int toSpecialtyId, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            return await _context.Referrals.AnyAsync(r =>
                r.PatientId == patientId
                && r.ToSpecialtyId == toSpecialtyId
                && !r.IsUsed
                && r.ExpiresAt > now,
                cancellationToken);
        }
    }
}