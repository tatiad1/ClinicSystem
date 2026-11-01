using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AppointmentService(
            IAppointmentRepository appointmentRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IDoctorScheduleRepository scheduleRepository,
            IReferralRepository referralRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _scheduleRepository = scheduleRepository;
            _referralRepository = referralRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved),
                    cancellationToken))
                throw new InvalidOperationException("Doctor already has an appointment at this time");

            if (await _appointmentRepository.ExistsAsync(a =>
                    a.PatientId == patient.Id
                    && a.StartTime == start
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved),
                    cancellationToken))
                throw new InvalidOperationException("Patient already has an appointment at this time");

            // 3. მიმართვა
            Referral? referral = null;

            if (patient.FamilyDoctorId == doctor.Id)
            {
                if (dto.ReferralId.HasValue)
                    throw new ArgumentException("Referral is not needed when booking the patient's family doctor");
            }
            else
            {
                if (!dto.ReferralId.HasValue)
                    throw new InvalidOperationException("A referral is required to book a doctor other than the family doctor");

                referral = await _referralRepository.GetByIdAsync(dto.ReferralId.Value, cancellationToken);
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
                Status = AppointmentStatus.Pending,
                ReferralId = referral?.Id
            };

            await _appointmentRepository.AddAsync(appointment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _appointmentRepository.GetWithDetailsAsync(appointment.Id, cancellationToken);
            return _mapper.Map<AppointmentDTO>(created);
        }

        // ---------- სტატუსები ----------

        public Task ApproveAsync(int id, CancellationToken cancellationToken)
            => ChangeStatusAsync(id, AppointmentStatus.Approved, cancellationToken);

        public Task RejectAsync(int id, CancellationToken cancellationToken)
            => ChangeStatusAsync(id, AppointmentStatus.Rejected, cancellationToken);

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

            if (newStatus == AppointmentStatus.Approved && appointment.StartTime <= now)
                throw new InvalidOperationException("Appointment time has already passed");

            if (newStatus == AppointmentStatus.Cancelled && appointment.StartTime <= now)
                throw new InvalidOperationException("Cannot cancel an appointment that has already started");

            if (newStatus == AppointmentStatus.NoShow && appointment.StartTime > now)
                throw new InvalidOperationException("Cannot mark no-show before the appointment time");

            appointment.Status = newStatus;

            // უარყოფილ ან გაუქმებულ ვიზიტზე მიმართვა ისევ თავისუფლდება
            if ((newStatus == AppointmentStatus.Rejected || newStatus == AppointmentStatus.Cancelled)
                && appointment.ReferralId.HasValue)
            {
                var referral = await _referralRepository.GetByIdAsync(appointment.ReferralId.Value, cancellationToken);
                if (referral != null)
                    referral.IsUsed = false;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static bool IsTransitionAllowed(AppointmentStatus from, AppointmentStatus to)
        {
            return from switch
            {
                AppointmentStatus.Pending => to is AppointmentStatus.Approved
                    or AppointmentStatus.Rejected
                    or AppointmentStatus.Cancelled,
                AppointmentStatus.Approved => to is AppointmentStatus.Cancelled
                    or AppointmentStatus.NoShow,
                _ => false
            };
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