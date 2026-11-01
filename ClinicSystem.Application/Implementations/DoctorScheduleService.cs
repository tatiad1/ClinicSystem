using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Implementations
{
    public class DoctorScheduleService : IDoctorScheduleService
    {
        private readonly IDoctorScheduleRepository _scheduleRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DoctorScheduleService(
            IDoctorScheduleRepository scheduleRepository,
            IDoctorRepository doctorRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _scheduleRepository = scheduleRepository;
            _doctorRepository = doctorRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

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

            _scheduleRepository.Delete(schedule);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
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