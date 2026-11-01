using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.Entities;
using ClinicSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Repositories
{
    public class PersonRepository : Repository<Person>, IPersonRepository
    {
        public PersonRepository(ClinicDbContext context) : base(context)
        {
        }

        public async Task<Person?> GetByPersonalNumberAsync(string personalNumber, CancellationToken cancellationToken = default)
        {
            return await _context.People.FirstOrDefaultAsync(p => p.PersonalNumber == personalNumber, cancellationToken);
        }
    }
}