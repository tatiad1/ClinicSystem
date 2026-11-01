using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface ISpecialtyRepository : IRepository<Specialty>
    {
        Task<bool> HasDoctorsAsync(int specialtyId, CancellationToken cancellationToken = default);
    }
}