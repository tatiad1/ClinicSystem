namespace ClinicSystem.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        int? DoctorId { get; }
        int? PatientId { get; }
        bool IsInRole(string role);

        // Doctors may access family patients or patients with an approved or completed appointment.
        Task EnsureCanAccessPatientAsync(int patientId, CancellationToken cancellationToken = default);

        // პერსონალს შეუძლია ნებისმიერი ექიმი, ექიმს მხოლოდ საკუთარი თავი
        void EnsureCanAccessDoctor(int doctorId);

        // პერსონალს შეუძლია ყველა ვიზიტი, ექიმსა და პაციენტს მხოლოდ საკუთარი
        void EnsureCanAccessAppointment(int patientId, int doctorId);
    }
}
