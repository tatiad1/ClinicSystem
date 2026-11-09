using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Implementations
{
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationRepository _consultationRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ConsultationService(
            IConsultationRepository consultationRepository,
            IAppointmentRepository appointmentRepository,
            IPatientRepository patientRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _consultationRepository = consultationRepository;
            _appointmentRepository = appointmentRepository;
            _patientRepository = patientRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ConsultationDTO?> GetByAppointmentAsync(int appointmentId, CancellationToken cancellationToken)
        {
            if (!await _appointmentRepository.ExistsAsync(a => a.Id == appointmentId, cancellationToken))
                throw new KeyNotFoundException("Appointment not found");

            var consultation = await _consultationRepository.GetByAppointmentIdAsync(appointmentId, cancellationToken);
            if (consultation == null)
                return null;

            return _mapper.Map<ConsultationDTO>(consultation);
        }

        public async Task<ICollection<ConsultationDTO>> GetByPatientAsync(int patientId, CancellationToken cancellationToken)
        {
            if (!await _patientRepository.ExistsAsync(p => p.Id == patientId, cancellationToken))
                throw new KeyNotFoundException("Patient not found");

            var consultations = await _consultationRepository.GetByPatientAsync(patientId, cancellationToken);
            return consultations.Select(_mapper.Map<ConsultationDTO>).ToList();
        }

        public async Task<ConsultationDTO> AddAsync(int appointmentId, CreateConsultationDTO dto, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found");

            if (await _consultationRepository.ExistsAsync(c => c.AppointmentId == appointmentId, cancellationToken))
                throw new InvalidOperationException("Consultation already exists for this appointment");

            if (appointment.Status != AppointmentStatus.Approved)
                throw new InvalidOperationException(
                    $"Consultation can only be added to an approved appointment (current status: {appointment.Status})");

            if (appointment.StartTime > DateTime.Now)
                throw new InvalidOperationException(
                    "Consultation cannot be recorded before the appointment starts");

            var diagnosis = ValidateDiagnosis(dto.Diagnosis);
            var prescription = NormalizePrescription(dto.Prescription);

            var consultation = new Consultation
            {
                AppointmentId = appointmentId,
                Diagnosis = diagnosis,
                Prescription = prescription
            };

            // ვიზიტი ხდება დასრულებული, ერთ შენახვაში კონსულტაციასთან ერთად
            appointment.Status = AppointmentStatus.Completed;

            await _consultationRepository.AddAsync(consultation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _consultationRepository.GetByAppointmentIdAsync(appointmentId, cancellationToken);
            return _mapper.Map<ConsultationDTO>(created);
        }

        public async Task UpdateAsync(int appointmentId, UpdateConsultationDTO dto, CancellationToken cancellationToken)
        {
            if (!await _appointmentRepository.ExistsAsync(a => a.Id == appointmentId, cancellationToken))
                throw new KeyNotFoundException("Appointment not found");

            var consultation = await _consultationRepository.GetByAppointmentIdAsync(appointmentId, cancellationToken);
            if (consultation == null)
                throw new KeyNotFoundException("Consultation not found");

            consultation.Diagnosis = ValidateDiagnosis(dto.Diagnosis);
            consultation.Prescription = NormalizePrescription(dto.Prescription);

            // ობიექტი უკვე თვალყურდევნებულია, ამიტომ Update() არ გვჭირდება
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static string ValidateDiagnosis(string? diagnosis)
        {
            if (string.IsNullOrWhiteSpace(diagnosis))
                throw new ArgumentException("Diagnosis is required");

            var trimmed = diagnosis.Trim();
            if (trimmed.Length > 1000)
                throw new ArgumentException("Diagnosis must be at most 1000 characters");

            return trimmed;
        }

        private static string? NormalizePrescription(string? prescription)
        {
            if (string.IsNullOrWhiteSpace(prescription))
                return null;

            var trimmed = prescription.Trim();
            if (trimmed.Length > 1000)
                throw new ArgumentException("Prescription must be at most 1000 characters");

            return trimmed;
        }
    }
}
