using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IReferralRepository : IRepository<Referral>
    {
        Task<Referral?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<Referral>> GetByPatientAsync(int patientId, bool activeOnly, CancellationToken cancellationToken = default);
        Task<bool> HasActiveReferralAsync(int patientId, int toSpecialtyId, CancellationToken cancellationToken = default);
    }
}