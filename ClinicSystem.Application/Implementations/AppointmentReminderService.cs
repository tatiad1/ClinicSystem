using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Settings;
using ClinicSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSystem.Application.Implementations
{
    public class AppointmentReminderService : IAppointmentReminderService
    {
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly INotificationSender _notificationSender;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ReminderSettings _settings;
        private readonly ILogger<AppointmentReminderService> _logger;

        public AppointmentReminderService(
            IAppointmentRepository appointmentRepository,
            INotificationSender notificationSender,
            IUnitOfWork unitOfWork,
            IOptions<ReminderSettings> options,
            ILogger<AppointmentReminderService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _notificationSender = notificationSender;
            _unitOfWork = unitOfWork;
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<int> SendDueRemindersAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.Now;
            var until = now.AddHours(_settings.HoursBefore);

            var appointments = await _appointmentRepository.GetDueForReminderAsync(now, until, cancellationToken);

            var sent = 0;

            foreach (var appointment in appointments)
            {
                var email = appointment.Patient.Person.User?.Email;
                if (string.IsNullOrWhiteSpace(email))
                    continue;

                var patientName = $"{appointment.Patient.Person.FirstName} {appointment.Patient.Person.LastName}";
                var doctorName = $"{appointment.Doctor.User.Person.FirstName} {appointment.Doctor.User.Person.LastName}";
                var specialty = appointment.Doctor.Specialty.Name;

                var subject = "Appointment reminder";
                var body = $"Dear {patientName}, this is a reminder about your appointment with " +
                           $"Dr. {doctorName} ({specialty}) on {appointment.StartTime:yyyy-MM-dd} at {appointment.StartTime:HH:mm}.";

                try
                {
                    await _notificationSender.SendAsync(email, subject, body, cancellationToken);
                    appointment.ReminderSent = true;
                    sent++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // არ ვნიშნავთ გაგზავნილად, მომდევნო შემოწმებაზე ისევ ეცდება
                    _logger.LogError(ex, "Failed to send reminder for appointment {AppointmentId}", appointment.Id);
                }
            }

            if (sent > 0)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            return sent;
        }
    }
}