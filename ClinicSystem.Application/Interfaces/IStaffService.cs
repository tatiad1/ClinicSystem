using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IStaffService
    {
        // role: Roles.Admin ან Roles.Receptionist
        Task<ICollection<StaffDTO>> GetStaffAsync(string role, CancellationToken cancellationToken);
        Task<StaffDTO> AddStaffAsync(string role, CreateStaffDTO dto, CancellationToken cancellationToken);

        // რეგისტრატორის მართვა შეუძლია ადმინს ან სუპერადმინს;
        // ადმინის მართვა — მხოლოდ სუპერადმინს.
        Task SetStaffActiveAsync(
            string userId,
            bool isActive,
            string currentUserId,
            bool currentUserIsSuperAdmin,
            CancellationToken cancellationToken);
    }
}
