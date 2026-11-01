using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IDoctorService
    {
        Task<ICollection<DoctorDTO>> GetAllAsync(int? specialtyId, CancellationToken cancellationToken);
        Task<DoctorDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<DoctorDTO> AddAsync(CreateDoctorDTO dto, CancellationToken cancellationToken);
        Task UpdateAsync(int id, UpdateDoctorDTO dto, CancellationToken cancellationToken);
        Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken);
    }
}