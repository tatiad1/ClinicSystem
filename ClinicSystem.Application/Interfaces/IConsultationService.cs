using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IConsultationService
    {
        Task<ConsultationDTO?> GetByAppointmentAsync(int appointmentId, CancellationToken cancellationToken);
        Task<ICollection<ConsultationDTO>> GetByPatientAsync(int patientId, CancellationToken cancellationToken);
        Task<ConsultationDTO> AddAsync(int appointmentId, CreateConsultationDTO dto, CancellationToken cancellationToken);
        Task UpdateAsync(int appointmentId, UpdateConsultationDTO dto, CancellationToken cancellationToken);
    }
}