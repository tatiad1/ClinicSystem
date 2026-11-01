using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IPatientRepository : IRepository<Patient>
    {
        Task<Patient?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<Patient>> GetAllWithDetailsAsync(int? familyDoctorId = null, CancellationToken cancellationToken = default);
    }
}