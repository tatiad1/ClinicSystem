using Microsoft.AspNetCore.Identity;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;
}