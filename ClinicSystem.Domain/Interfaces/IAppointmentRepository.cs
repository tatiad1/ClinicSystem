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

        // ექიმის მომავალი აქტიური ვიზიტების დროები
        Task<ICollection<DateTime>> GetFutureActiveStartTimesAsync(int doctorId, DateTime from, CancellationToken cancellationToken = default);

        // დადასტურებული ვიზიტები, რომლებზეც შეხსენება ჯერ არ გაგზავნილა და პაციენტს ანგარიში აქვს
        Task<ICollection<Appointment>> GetDueForReminderAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    }
}