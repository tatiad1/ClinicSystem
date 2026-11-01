using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IDoctorScheduleService
    {
        Task<ICollection<DoctorScheduleDTO>> GetByDoctorAsync(int doctorId, CancellationToken cancellationToken);
        Task<DoctorScheduleDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<DoctorScheduleDTO> AddAsync(int doctorId, CreateDoctorScheduleDTO dto, CancellationToken cancellationToken);
        Task UpdateAsync(int id, UpdateDoctorScheduleDTO dto, CancellationToken cancellationToken);
        Task DeleteAsync(int id, CancellationToken cancellationToken);
    }
}