using ClinicSystem.Domain.Models.DTOs;

namespace ClinicSystem.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentDTO?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<ICollection<AppointmentDTO>> GetByPatientAsync(int patientId, CancellationToken cancellationToken);
        Task<ICollection<AppointmentDTO>> GetByDoctorAsync(int doctorId, DateOnly? date, CancellationToken cancellationToken);
        Task<ICollection<AvailableSlotDTO>> GetAvailableSlotsAsync(int doctorId, DateOnly date, CancellationToken cancellationToken);

        Task<AppointmentDTO> AddAsync(CreateAppointmentDTO dto, CancellationToken cancellationToken);

        Task CancelAsync(int id, CancellationToken cancellationToken);
        Task MarkNoShowAsync(int id, CancellationToken cancellationToken);
    }
}
