using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Domain.Interfaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<Appointment?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<Appointment>> GetByPatientAsync(int patientId, CancellationToken cancellationToken = default);
        Task<ICollection<Appointment>> GetByDoctorAsync(int doctorId, DateOnly? date, CancellationToken cancellationToken = default);

        // ექიმის აქტიური (Pending/Approved) ვიზიტების დაწყების დროები ერთ დღეში
        Task<ICollection<DateTime>> GetActiveStartTimesAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
    }
}