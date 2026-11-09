using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Implementations
{
    public class DoctorScheduleService : IDoctorScheduleService
    {
        private readonly IDoctorScheduleRepository _scheduleRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DoctorScheduleService(
            IDoctorScheduleRepository scheduleRepository,
            IDoctorRepository doctorRepository,
            IAppointmentRepository appointmentRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _scheduleRepository = scheduleRepository;
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        private record ScheduleWindow(DayOfWeek Day, TimeOnly Start, TimeOnly End);

        public async Task<ICollection<DoctorScheduleDTO>> GetByDoctorAsync(int doctorId, CancellationToken cancellationToken)
        {
            if (!await _doctorRepository.ExistsAsync(d => d.Id == doctorId, cancellationToken))
                throw new KeyNotFoundException("Doctor not found");

            var schedules = await _scheduleRepository.GetByDoctorAsync(doctorId, cancellationToken);
            return schedules.Select(_mapper.Map<DoctorScheduleDTO>).ToList();
        }

        public async Task<DoctorScheduleDTO?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
            if (schedule == null)
                return null;

            return _mapper.Map<DoctorScheduleDTO>(schedule);
        }

        public async Task<DoctorScheduleDTO> AddAsync(int doctorId, CreateDoctorScheduleDTO dto, CancellationToken cancellationToken)
        {
            if (!await _doctorRepository.ExistsAsync(d => d.Id == doctorId, cancellationToken))
                throw new KeyNotFoundException("Doctor not found");

            ValidateTimes(dto.DayOfWeek, dto.StartTime, dto.EndTime);

            if (await _scheduleRepository.HasOverlapAsync(doctorId, dto.DayOfWeek, dto.StartTime, dto.EndTime,
                    null, cancellationToken))
                throw new InvalidOperationException("Schedule overlaps with an existing schedule on this day");

            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime
            };

            await _scheduleRepository.AddAsync(schedule, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return _mapper.Map<DoctorScheduleDTO>(schedule);
        }

        public async Task UpdateAsync(int id, UpdateDoctorScheduleDTO dto, CancellationToken cancellationToken)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
            if (schedule == null)
                throw new KeyNotFoundException("Schedule not found");

            ValidateTimes(dto.DayOfWeek, dto.StartTime, dto.EndTime);

            if (await _scheduleRepository.HasOverlapAsync(schedule.DoctorId, dto.DayOfWeek, dto.StartTime, dto.EndTime,
                    id, cancellationToken))
                throw new InvalidOperationException("Schedule overlaps with an existing schedule on this day");

            // ცვლილების შემდეგ მომავალ ვიზიტებს გრაფიკი არ უნდა დაუკარგოს
            await EnsureNoOrphanedAppointmentsAsync(
                schedule.DoctorId,
                id,
                new ScheduleWindow(dto.DayOfWeek, dto.StartTime, dto.EndTime),
                cancellationToken);

            schedule.DayOfWeek = dto.DayOfWeek;
            schedule.StartTime = dto.StartTime;
            schedule.EndTime = dto.EndTime;

            _scheduleRepository.Update(schedule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, CancellationToken cancellationToken)
        {
            var schedule = await _scheduleRepository.GetByIdAsync(id, cancellationToken);
            if (schedule == null)
                throw new KeyNotFoundException("Schedule not found");

            await EnsureNoOrphanedAppointmentsAsync(schedule.DoctorId, id, null, cancellationToken);

            _scheduleRepository.Delete(schedule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // replacement = null ნიშნავს წაშლას; სხვა შემთხვევაში გრაფიკის ახალ მნიშვნელობას
        private async Task EnsureNoOrphanedAppointmentsAsync(
            int doctorId, int changedScheduleId, ScheduleWindow? replacement, CancellationToken cancellationToken)
        {
            var upcoming = await _appointmentRepository.GetFutureActiveStartTimesAsync(
                doctorId, DateTime.Now, cancellationToken);

            if (upcoming.Count == 0)
                return;

            var schedules = await _scheduleRepository.GetByDoctorAsync(doctorId, cancellationToken);

            // გრაფიკები ცვლილების შემდეგ
            var windows = schedules
                .Where(s => s.Id != changedScheduleId)
                .Select(s => new ScheduleWindow(s.DayOfWeek, s.StartTime, s.EndTime))
                .ToList();

            if (replacement != null)
                windows.Add(replacement);

            var slot = TimeSpan.FromMinutes(AppointmentRules.SlotMinutes);

            foreach (var start in upcoming)
            {
                var from = TimeOnly.FromDateTime(start);
                var to = from.Add(slot);

                var covered = windows.Any(w => w.Day == start.DayOfWeek && w.Start <= from && w.End >= to);
                if (!covered)
                    throw new InvalidOperationException(
                        "Doctor has upcoming appointments in this time range. Cancel or reassign them first");
            }
        }

        private static void ValidateTimes(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
        {
            if (!Enum.IsDefined(dayOfWeek))
                throw new ArgumentException("Invalid day of week");

            if (endTime <= startTime)
                throw new ArgumentException("End time must be later than start time");
        }
    }
}