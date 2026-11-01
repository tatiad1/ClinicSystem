using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IPatientService
    {
        Task<ICollection<PatientDTO>> GetAllAsync(int? familyDoctorId, CancellationToken cancellationToken);
        Task<PatientDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<PatientDTO> AddAsync(CreatePatientDTO dto, CancellationToken cancellationToken);
        Task UpdateAsync(int id, UpdatePatientDTO dto, CancellationToken cancellationToken);
        Task AssignFamilyDoctorAsync(int id, int? doctorId, CancellationToken cancellationToken);
    }
}