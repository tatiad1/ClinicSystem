using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IMedicalProfileRepository : IRepository<MedicalProfile>
    {
        Task<MedicalProfile?> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken = default);
    }
}