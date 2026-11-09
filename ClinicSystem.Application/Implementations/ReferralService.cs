using AutoMapper;
using ClinicSystem.Application.Interfaces;
using ClinicSystem.Domain.Enums;
using ClinicSystem.Domain.Interfaces;
using ClinicSystem.Domain.Models.DTOs;
using ClinicSystem.Domain.Models.Entities;

namespace ClinicSystem.Application.Implementations
{
    public class ReferralService : IReferralService
    {
        private const int DefaultValidDays = 30;
        private const int MaxValidDays = 365;

        private readonly IReferralRepository _referralRepository;
        private readonly IPatientRepository _patientRepository;
        private readonly IDoctorRepository _doctorRepository;
        private readonly IAppointmentRepository _appointmentRepository;
        private readonly ISpecialtyRepository _specialtyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ReferralService(
            IReferralRepository referralRepository,
            IPatientRepository patientRepository,
            IDoctorRepository doctorRepository,
            IAppointmentRepository appointmentRepository,
            ISpecialtyRepository specialtyRepository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _referralRepository = referralRepository;
            _patientRepository = patientRepository;
            _doctorRepository = doctorRepository;
            _appointmentRepository = appointmentRepository;
            _specialtyRepository = specialtyRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ICollection<ReferralDTO>> GetByPatientAsync(int patientId, bool activeOnly, CancellationToken cancellationToken)
        {
            if (!await _patientRepository.ExistsAsync(p => p.Id == patientId, cancellationToken))
                throw new KeyNotFoundException("Patient not found");

            var referrals = await _referralRepository.GetByPatientAsync(patientId, activeOnly, cancellationToken);
            return referrals.Select(_mapper.Map<ReferralDTO>).ToList();
        }

        public async Task<ReferralDTO?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var referral = await _referralRepository.GetWithDetailsAsync(id, cancellationToken);
            if (referral == null)
                return null;

            return _mapper.Map<ReferralDTO>(referral);
        }

        public async Task<ReferralDTO> AddAsync(int patientId, CreateReferralDTO dto, CancellationToken cancellationToken)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId, cancellationToken);
            if (patient == null)
                throw new KeyNotFoundException("Patient not found");

            var doctor = await _doctorRepository.GetByIdAsync(dto.FromDoctorId, cancellationToken);
            if (doctor == null)
                throw new KeyNotFoundException("Doctor not found");

            if (!doctor.IsActive)
                throw new InvalidOperationException("Doctor is not active");

            var isFamilyDoctor = patient.FamilyDoctorId == doctor.Id;
            var hasCompletedConsultation = await _appointmentRepository.ExistsAsync(a =>
                a.PatientId == patientId
                && a.DoctorId == doctor.Id
                && a.Status == AppointmentStatus.Completed,
                cancellationToken);

            if (!isFamilyDoctor && !hasCompletedConsultation)
                throw new InvalidOperationException(
                    "Only the patient's family doctor or a doctor who completed a consultation can create a referral");

            var targetSpecialty = await _specialtyRepository.GetByIdAsync(dto.ToSpecialtyId, cancellationToken);
            if (targetSpecialty == null)
                throw new KeyNotFoundException("Specialty not found");

            if (dto.ToSpecialtyId == doctor.SpecialtyId)
                throw new ArgumentException("Cannot refer a patient to the doctor's own specialty");

            if (!targetSpecialty.RequiresReferral)
                throw new InvalidOperationException("This specialty does not require a referral");

            var validDays = dto.ValidForDays ?? DefaultValidDays;
            if (validDays < 1 || validDays > MaxValidDays)
                throw new ArgumentException($"ValidForDays must be between 1 and {MaxValidDays}");

            var reason = NormalizeReason(dto.Reason);

            if (await _referralRepository.HasActiveReferralAsync(patientId, dto.ToSpecialtyId, cancellationToken))
                throw new InvalidOperationException("Patient already has an active referral to this specialty");

            var referral = new Referral
            {
                PatientId = patientId,
                FromDoctorId = doctor.Id,
                ToSpecialtyId = dto.ToSpecialtyId,
                Reason = reason,
                ExpiresAt = DateTime.UtcNow.AddDays(validDays),
                IsUsed = false
            };

            await _referralRepository.AddAsync(referral, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var created = await _referralRepository.GetWithDetailsAsync(referral.Id, cancellationToken);
            return _mapper.Map<ReferralDTO>(created);
        }

        private static string? NormalizeReason(string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return null;

            var trimmed = reason.Trim();
            if (trimmed.Length > 500)
                throw new ArgumentException("Reason must be at most 500 characters");

            return trimmed;
        }
    }
}
