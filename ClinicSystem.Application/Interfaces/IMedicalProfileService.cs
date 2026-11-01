using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IMedicalProfileService
    {
        Task<MedicalProfileDTO?> GetByPatientIdAsync(int patientId, CancellationToken cancellationToken);
        Task<MedicalProfileDTO> AddAsync(int patientId, CreateMedicalProfileDTO dto, CancellationToken cancellationToken);
        Task UpdateAsync(int patientId, UpdateMedicalProfileDTO dto, CancellationToken cancellationToken);
    }
}