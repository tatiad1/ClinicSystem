using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IDoctorRepository : IRepository<Doctor>
    {
        Task<Doctor?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<Doctor>> GetAllWithDetailsAsync(int? specialtyId = null, CancellationToken cancellationToken = default);
    }
}