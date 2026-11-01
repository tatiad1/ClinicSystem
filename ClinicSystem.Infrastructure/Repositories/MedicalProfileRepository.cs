using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class MedicalProfileRepository : Repository<MedicalProfile>, IMedicalProfileRepository
    {
        public MedicalProfileRepository(ClinicDbContext context) : base(context)
        {
        }

        public async Task<MedicalProfile?> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default)
        {
            return await _context.MedicalProfiles
                .FirstOrDefaultAsync(m => m.PatientId == patientId, cancellationToken);
        }
    }
}