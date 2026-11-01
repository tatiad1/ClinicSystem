using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IReferralService
    {
        Task<ICollection<ReferralDTO>> GetByPatientAsync(int patientId, bool activeOnly, CancellationToken cancellationToken);
        Task<ReferralDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<ReferralDTO> AddAsync(int patientId, CreateReferralDTO dto, CancellationToken cancellationToken);
    }
}