using ClinicSystem.Domain.Models.Identity;

namespace ClinicSystem.Domain.Models.Entities;

public class Person : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PersonalNumber { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? ContactEmail { get; set; }

    public ApplicationUser? User { get; set; }
    public Patient? Patient { get; set; }
}
