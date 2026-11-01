using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IConsultationRepository : IRepository<Consultation>
    {
        Task<Consultation?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
        Task<ICollection<Consultation>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default);
    }
}