using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDTO> LoginAsync(LoginDTO dto, CancellationToken cancellationToken);
        Task<PatientDTO> RegisterPatientAsync(RegisterDTO dto, CancellationToken cancellationToken);
        Task ChangePasswordAsync(string userId, ChangePasswordDTO dto, CancellationToken cancellationToken);
    }
}