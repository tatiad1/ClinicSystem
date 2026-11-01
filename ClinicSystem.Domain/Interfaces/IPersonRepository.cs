using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IPersonRepository : IRepository<Person>
    {
        Task<Person?> GetByPersonalNumberAsync(string personalNumber, CancellationToken cancellationToken = default);
    }
}