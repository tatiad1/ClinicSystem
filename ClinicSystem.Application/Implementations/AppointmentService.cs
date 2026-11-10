using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Application.Settings;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicSystem.Application.Implementations
{
    public class AppointmentService : IAppointmentService
    {
        private const int SlotMinutes = AppointmentRules.SlotMinutes;

        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IDoctorScheduleRepository _scheduleRepository;
        private readonly IReferralRepository _referralRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IEmailNotificationQueue _emailNotificationQueue;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IDoctorScheduleRepository scheduleRepository,
            IReferralRepository referralRepository,
            ISpecialtyRepository specialtyRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IEmailNotificationQueue emailNotificationQueue,
            IOptions<EmailSettings> emailOptions,
            ILogger<AppointmentService> logger)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _scheduleRepository = scheduleRepository;
            _referralRepository = referralRepository;
            _specialtyRepository = specialtyRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _emailNotificationQueue = emailNotificationQueue;
            _emailSettings = emailOptions.Value;
            _logger = logger;
        }

        // ---------- წაკითხვა ----------

        public async Task<AppointmentDTO?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentRepository.GetWithDetailsAsync(id, cancellationToken);
            if (appointment == null)
                return null;

            return _mapper.Map<AppointmentDTO>(appointment);
        }

        public async Task<ICollection<AppointmentDTO>> GetByPatientAsync(int patientId, CancellationToken cancellationToken)
        {
            if (!await _patientRepository.ExistsAsync(p => p.Id == patientId, cancellationToken))
                throw new KeyNotFoundException("Patient not found");

            var appointments = await _appointmentRepository.GetByPatientAsync(patientId, cancellationToken);
            return appointments.Select(_mapper.Map<AppointmentDTO>).ToList();
        }

        public async Task<ICollection<AppointmentDTO>> GetByDoctorAsync(int doctorId, DateOnly? date, CancellationToken cancellationToken)
        {
            if (!await _doctorRepository.ExistsAsync(d => d.Id == doctorId, cancellationToken))
                throw new KeyNotFoundException("Doctor not found");

            var appointments = await _appointmentRepository.GetByDoctorAsync(doctorId, date, cancellationToken);
            return appointments.Select(_mapper.Map<AppointmentDTO>).ToList();
        }

        public async Task<ICollection<AvailableSlotDTO>> GetAvailableSlotsAsync(int doctorId, DateOnly date, CancellationToken cancellationToken)
        {
            var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken);
            if (doctor == null)
                throw new KeyNotFoundException("Doctor not found");

            if (!doctor.IsActive)
                throw new InvalidOperationException("Doctor is not active");

            var schedules = (await _scheduleRepository.GetByDoctorAsync(doctorId, cancellationToken))
                .Where(s => s.DayOfWeek == date.DayOfWeek)
                .ToList();

            var taken = (await _appointmentRepository.GetActiveStartTimesAsync(doctorId, date, cancellationToken))
                .ToHashSet();

            var now = DateTime.Now;
            var dayStart = date.ToDateTime(TimeOnly.MinValue);
            var slots = new List<AvailableSlotDTO>();

            foreach (var schedule in schedules)
            {
                // პირველი ბადის წერტილი, რომელიც გრაფიკის დაწყების შემდეგაა
                var firstMinute = (int)Math.Ceiling(schedule.StartTime.ToTimeSpan().TotalMinutes / SlotMinutes) * SlotMinutes;
                var endMinute = (int)schedule.EndTime.ToTimeSpan().TotalMinutes;

                for (var minute = firstMinute; minute + SlotMinutes <= endMinute; minute += SlotMinutes)
                {
                    var start = dayStart.AddMinutes(minute);

                    if (start > now && !taken.Contains(start))
                    {
                        slots.Add(new AvailableSlotDTO
                        {
                            StartTime = start,
                            EndTime = start.AddMinutes(SlotMinutes)
                        });
                    }
                }
            }

            return slots.OrderBy(s => s.StartTime).ToList();
        }

        // ---------- ჩაწერა ----------

        public async Task<AppointmentDTO> AddAsync(CreateAppointmentDTO dto, CancellationToken cancellationToken)
        {
            var patient = await _patientRepository.GetByIdAsync(dto.PatientId, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            var doctor = await _doctorRepository.GetByIdAsync(dto.DoctorId, cancellationToken);
            if (doctor == null)
                throw new KeyNotFoundException("Doctor not found");

            if (!doctor.IsActive)
                throw new InvalidOperationException("Doctor is not active");

            var start = dto.StartTime;
            ValidateSlot(start);

            // 1. ექიმი ამ დროს მუშაობს?
            var schedules = await _scheduleRepository.GetByDoctorAsync(doctor.Id, cancellationToken);
            var startTime = TimeOnly.FromDateTime(start);
            var endTime = TimeOnly.FromDateTime(start.AddMinutes(SlotMinutes));

            var worksAtThisTime = endTime > startTime
                && schedules.Any(s => s.DayOfWeek == start.DayOfWeek
                    && s.StartTime <= startTime
                    && s.EndTime >= endTime);

            if (!worksAtThisTime)
                throw new InvalidOperationException("Doctor does not work at this time");

            // 2. კონფლიქტები
            if (await _appointmentRepository.ExistsAsync(a =>
                    a.DoctorId == doctor.Id
                    && a.StartTime == start
                    && a.Status == AppointmentStatus.Booked,
                    cancellationToken))
                throw new InvalidOperationException("Doctor already has an appointment at this time");

            if (await _appointmentRepository.ExistsAsync(a =>
                    a.PatientId == patient.Id
                    && a.StartTime == start
                    && a.Status == AppointmentStatus.Booked,
                    cancellationToken))
                throw new InvalidOperationException("Patient already has an appointment at this time");

            // 3. Referral policy comes from the doctor's specialty. Only Family Medicine
            // visits are exempt; assigning a non-family specialist must not bypass this rule.
            Referral? referral = null;
            var specialty = await _specialtyRepository.GetByIdAsync(doctor.SpecialtyId, cancellationToken)
                ?? throw new KeyNotFoundException("Doctor specialty not found");

            var referralRequired = specialty.RequiresReferral
                && !specialty.IsFamilyMedicine;

            if (!referralRequired)
            {
                if (dto.ReferralId.HasValue)
                    throw new ArgumentException("A referral is not needed for this specialty");
            }
            else
            {
                if (!dto.ReferralId.HasValue)
                {
                    var message = patient.FamilyDoctorId.HasValue
                        ? "A referral is required for this specialty"
                        : "Please book a family-medicine appointment and obtain a referral before visiting this specialty";
                    throw new InvalidOperationException(message);
                }

                referral = await _referralRepository.GetByIdAsync(
                    dto.ReferralId.Value, cancellationToken);

                if (referral == null)
                    throw new KeyNotFoundException("Referral not found");

                if (referral.PatientId != patient.Id)
                    throw new InvalidOperationException("Referral belongs to another patient");

                if (referral.IsUsed)
                    throw new InvalidOperationException("Referral is already used");

                if (referral.ExpiresAt <= DateTime.UtcNow)
                    throw new InvalidOperationException("Referral has expired");

                if (referral.ToSpecialtyId != doctor.SpecialtyId)
                    throw new InvalidOperationException("Referral is for a different specialty");

                referral.IsUsed = true;
            }

            var appointment = new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                StartTime = start,
                Status = AppointmentStatus.Booked,
                ReferralId = referral?.Id
            };

            await _appointmentRepository.AddAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _appointmentRepository.GetWithDetailsAsync(appointment.Id, cancellationToken);
            if (created != null)
                await SendBookingConfirmationAsync(created, cancellationToken);

            return _mapper.Map<AppointmentDTO>(created);
        }

        private async Task SendBookingConfirmationAsync(Appointment appointment, CancellationToken cancellationToken)
        {
            if (!_emailSettings.Enabled)
                return;

            var patient = appointment.Patient;
            var email = string.IsNullOrWhiteSpace(patient.Person.ContactEmail)
                ? patient.Person.User?.Email
                : patient.Person.ContactEmail;
            if (string.IsNullOrWhiteSpace(email))
                return;

            var patientName = $"{patient.Person.FirstName} {patient.Person.LastName}";
            var doctorName = $"{appointment.Doctor.User.Person.FirstName} {appointment.Doctor.User.Person.LastName}";
            var subject = "Appointment booked";
            var body = $"Dear {patientName}, your appointment is booked with Dr. {doctorName} " +
                       $"({appointment.Doctor.Specialty.Name}) on {appointment.StartTime:yyyy-MM-dd} " +
                       $"at {appointment.StartTime:HH:mm}.";

            try
            {
                await _emailNotificationQueue.EnqueueAsync(
                    email.Trim(), subject, body, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // The appointment is already saved; an email failure must not undo the booking.
                _logger.LogError(ex, "Failed to send booking confirmation for appointment {AppointmentId}", appointment.Id);
            }
        }

        // ---------- სტატუსები ----------

        public Task CancelAsync(int id, CancellationToken cancellationToken)
            => ChangeStatusAsync(id, AppointmentStatus.Cancelled, cancellationToken);

        public Task MarkNoShowAsync(int id, CancellationToken cancellationToken)
            => ChangeStatusAsync(id, AppointmentStatus.NoShow, cancellationToken);

        private async Task ChangeStatusAsync(int id, AppointmentStatus newStatus, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(id, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            if (!IsTransitionAllowed(appointment.Status, newStatus))
                throw new InvalidOperationException($"Cannot change status from {appointment.Status} to {newStatus}");

            var now = DateTime.Now;

            if (newStatus == AppointmentStatus.Cancelled && appointment.StartTime <= now)
                throw new InvalidOperationException("Cannot cancel an appointment that has already started");

            var noShowAllowedAt = appointment.StartTime.AddMinutes(
                AppointmentRules.SlotMinutes + AppointmentRules.NoShowGraceMinutes);
            if (newStatus == AppointmentStatus.NoShow && noShowAllowedAt > now)
                throw new InvalidOperationException(
                    "Cannot mark no-show until the appointment has ended and the grace period has passed");

            appointment.Status = newStatus;

            // გაუქმებულ ვიზიტზე მიმართვა ისევ თავისუფლდება
            if (newStatus == AppointmentStatus.Cancelled && appointment.ReferralId.HasValue)
            {
                var referral = await _referralRepository.GetByIdAsync(appointment.ReferralId.Value, cancellationToken);
                if (referral != null)
                    referral.IsUsed = false;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static bool IsTransitionAllowed(AppointmentStatus from, AppointmentStatus to)
        {
            return from == AppointmentStatus.Booked
                && (to == AppointmentStatus.Cancelled || to == AppointmentStatus.NoShow);
        }

        private static void ValidateSlot(DateTime start)
        {
            if (start <= DateTime.Now)
                throw new ArgumentException("Appointment time must be in the future");

            if (start.Second != 0 || start.Millisecond != 0 || start.Minute % SlotMinutes != 0)
                throw new ArgumentException($"Appointment must start on a {SlotMinutes}-minute slot (for example 10:00 or 10:30)");
        }
    }
}
