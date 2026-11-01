using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface ISpecialtyService
    {
        Task<ICollection<SpecialtyDTO>> GetAllAsync(CancellationToken cancellationToken);
        Task<SpecialtyDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<SpecialtyDTO> AddAsync(CreateSpecialtyDTO dto, CancellationToken cancellationToken);
        Task UpdateAsync(int id, UpdateSpecialtyDTO dto, CancellationToken cancellationToken);
        Task DeleteAsync(int id, CancellationToken cancellationToken);
    }
}